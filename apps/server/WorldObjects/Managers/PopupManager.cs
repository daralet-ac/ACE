using System.Collections.Generic;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameEvent.Events;

namespace ACE.Server.WorldObjects.Managers;

/// <summary>
/// Batches GameEventPopupString messages for a player instead of sending them one at a time.
/// Some moments (e.g. logging in with many starter tasks) grant a dozen contracts at once -
/// popups queued within a short window of each other are combined into a single popup instead
/// of stacking a wall of separate boxes, the same way WorldManager merges the login MOTD/
/// welcome text into one popup. Completed-task lines are always listed before new-task lines,
/// regardless of the order the events actually fired in.
/// </summary>
public class PopupManager
{
    private readonly Player Player;

    private readonly object queueLock = new object();

    private readonly List<string> completedMessages = new List<string>();

    private readonly List<string> newTaskNames = new List<string>();

    private bool flushScheduled;

    private const double BatchWindowSeconds = 1.0;

    public PopupManager(Player player)
    {
        Player = player;
    }

    /// <summary>
    /// Queues a "task completed" popup line. Shown above any queued "new task" lines
    /// in the same batch.
    /// </summary>
    public void EnqueueTaskCompleted(string contractName)
    {
        Enqueue(completedMessages, $"Task Completed: {contractName}");
    }

    /// <summary>
    /// Queues a new-task contract name. Shown below any queued "task completed" lines
    /// in the same batch, as "New Task: X" if it's the only one, or as a "New Tasks:"
    /// header followed by a single comma-separated line if there are several.
    /// </summary>
    public void EnqueueNewTask(string contractName)
    {
        Enqueue(newTaskNames, contractName);
    }

    private void Enqueue(List<string> messages, string message)
    {
        lock (queueLock)
        {
            messages.Add(message);

            if (flushScheduled)
            {
                return;
            }

            flushScheduled = true;
        }

        var actionChain = new ActionChain();
        actionChain.AddDelaySeconds(BatchWindowSeconds);
        actionChain.AddAction(Player, Flush);
        actionChain.EnqueueChain();
    }

    private void Flush()
    {
        List<string> completed;
        List<string> newTasks;

        lock (queueLock)
        {
            completed = new List<string>(completedMessages);
            newTasks = new List<string>(newTaskNames);

            completedMessages.Clear();
            newTaskNames.Clear();
            flushScheduled = false;
        }

        if (completed.Count == 0 && newTasks.Count == 0)
        {
            return;
        }

        // Single-space within each section; a blank line only between the two sections.
        var sections = new List<string>();

        if (completed.Count > 0)
        {
            sections.Add(string.Join("\n", completed));
        }

        if (newTasks.Count == 1)
        {
            sections.Add($"New Task: {newTasks[0]}");
        }
        else if (newTasks.Count > 1)
        {
            sections.Add($"New Tasks:\n{string.Join(", ", newTasks)}");
        }

        Player.Session.Network.EnqueueSend(
            new GameEventPopupString(Player.Session, string.Join("\n\n", sections))
        );
    }
}
