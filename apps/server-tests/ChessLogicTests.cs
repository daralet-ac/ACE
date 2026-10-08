using System.Collections.Generic;
using System.Linq;
using System.Text;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity.Chess;
using ACE.Server.Entity.Chess.Pieces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class ChessLogicTests
{
    private const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";

    // the standard perft test positions, with their published move counts:
    // https://www.chessprogramming.org/Perft_Results
    private const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -";
    private const string Position3 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - -";

    #region Move generation

    [TestMethod]
    public void Perft_TheStartingPositionHasTheRightNumberOfMoves()
    {
        var logic = FromFen(StartingPosition);

        Assert.AreEqual(20, Perft(logic, 1));
        Assert.AreEqual(400, Perft(logic, 2));
        Assert.AreEqual(8902, Perft(logic, 3));
    }

    [TestMethod]
    public void Perft_KiwipeteHasTheRightNumberOfMoves()
    {
        // castling both ways, captures, pins and en passant
        var logic = FromFen(Kiwipete);

        Assert.AreEqual(48, Perft(logic, 1));
        Assert.AreEqual(2039, Perft(logic, 2));
        Assert.AreEqual(97862, Perft(logic, 3));
    }

    [TestMethod]
    public void Perft_Position3HasTheRightNumberOfMoves()
    {
        // en passant, discovered checks and pins in an endgame
        var logic = FromFen(Position3);

        Assert.AreEqual(14, Perft(logic, 1));
        Assert.AreEqual(191, Perft(logic, 2));
        Assert.AreEqual(2812, Perft(logic, 3));
    }

    [TestMethod]
    public void GenerateMoves_ListsEachMoveOnce()
    {
        var logic = FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq -");

        var moves = new List<ChessMove>();
        logic.GenerateMoves(ChessColor.White, moves);

        var duplicates = moves
            .GroupBy(m => (m.From.ToString(), m.To.ToString(), m.Flags))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Item1}-{g.Key.Item2} x{g.Count()}")
            .ToList();

        Assert.AreEqual(0, duplicates.Count, string.Join(", ", duplicates));
        Assert.AreEqual(1, moves.Count(m => m.Flags.HasFlag(ChessMoveFlag.KingSideCastle)));
        Assert.AreEqual(1, moves.Count(m => m.Flags.HasFlag(ChessMoveFlag.QueenSideCastle)));
    }

    #endregion

    #region Undo

    [DataTestMethod]
    [DataRow(Kiwipete, DisplayName = "castling, captures")]
    [DataRow("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq -", DisplayName = "black castling")]
    [DataRow("rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6", DisplayName = "en passant")]
    [DataRow("r3k3/1P6/8/8/8/8/8/4K3 w q -", DisplayName = "promotion, capturing promotion")]
    [DataRow("4k3/8/8/8/8/8/1p6/R3K3 b Q -", DisplayName = "black promotion")]
    public void UndoMove_PutsEverythingBackForEveryMove(string fen)
    {
        var logic = FromFen(fen);

        var moves = new List<ChessMove>();
        logic.GenerateMoves(logic.Turn, moves);

        Assert.IsTrue(moves.Count > 0);

        var before = Snapshot(logic);

        foreach (var move in moves)
        {
            logic.FinalizeMove(move);
            logic.UndoMove(1);

            Assert.AreEqual(before, Snapshot(logic), $"{move.Flags} {move.From}-{move.To}");
        }
    }

    [TestMethod]
    public void UndoMove_ThenTheSameMoveAgainGivesTheSameBoard()
    {
        // the AI plays the move it liked best after having undone it
        var logic = FromFen(Kiwipete);

        var moves = new List<ChessMove>();
        logic.GenerateMoves(logic.Turn, moves);

        foreach (var move in moves)
        {
            logic.FinalizeMove(move);
            var after = Snapshot(logic);

            logic.UndoMove(1);
            logic.FinalizeMove(move);

            Assert.AreEqual(after, Snapshot(logic), $"{move.Flags} {move.From}-{move.To}");

            logic.UndoMove(1);
        }
    }

    #endregion

    #region Castling

    [TestMethod]
    public void Castling_CapturingARookOnItsSquareTakesAwayThatCastle()
    {
        var logic = FromFen("r3k2r/8/8/8/8/8/1B6/4K3 w kq -");

        Assert.IsTrue(logic.DoMove(ChessColor.White, Square("b2"), Square("h8")) > ChessMoveResult.NoMoveResult);

        Assert.AreEqual(ChessMoveFlag.QueenSideCastle, logic.Castling[(int)ChessColor.Black]);
        Assert.IsFalse(KingMoves(logic, ChessColor.Black).Any(m => m.Flags.HasFlag(ChessMoveFlag.KingSideCastle)));

        // still not once the bishop has left the rook's square
        logic.DoMove(ChessColor.Black, Square("a8"), Square("a7"));
        logic.DoMove(ChessColor.White, Square("h8"), Square("d4"));

        Assert.IsFalse(KingMoves(logic, ChessColor.Black).Any(m => m.Flags.HasFlag(ChessMoveFlag.KingSideCastle)));
        Assert.AreEqual(
            ChessMoveResult.BadMoveInvalidCommand,
            logic.DoMove(ChessColor.Black, Square("e8"), Square("g8"))
        );
    }

    [TestMethod]
    public void Castling_CantCastleWithARookThatIsntThere()
    {
        // the castle right says yes, but the rook is gone
        var logic = FromFen("r3k3/8/8/8/8/8/8/4K3 b kq -");

        Assert.IsFalse(KingMoves(logic, ChessColor.Black).Any(m => m.Flags.HasFlag(ChessMoveFlag.KingSideCastle)));
        Assert.IsTrue(KingMoves(logic, ChessColor.Black).Any(m => m.Flags.HasFlag(ChessMoveFlag.QueenSideCastle)));
    }

    [DataTestMethod]
    [DataRow("4kr2/8/8/8/8/8/8/R3K2R w KQ -", false, true, DisplayName = "through an attacked square")]
    [DataRow("4k1r1/8/8/8/8/8/8/R3K2R w KQ -", false, true, DisplayName = "onto an attacked square")]
    [DataRow("4r1k1/8/8/8/8/8/8/R3K2R w KQ -", false, false, DisplayName = "out of check")]
    [DataRow("1r2k3/8/8/8/8/8/8/R3K2R w KQ -", true, true, DisplayName = "queenside, past an attacked b1")]
    [DataRow("2b1k3/8/8/8/8/8/8/R3K2R w KQ -", true, true, DisplayName = "a bishop off the king's path")]
    [DataRow("4k3/8/8/8/b7/8/8/R3K2R w KQ -", true, false, DisplayName = "a bishop on the queenside path")]
    public void Castling_NeverPassesThroughCheck(string fen, bool kingSide, bool queenSide)
    {
        var logic = FromFen(fen);
        var moves = KingMoves(logic, ChessColor.White);

        Assert.AreEqual(kingSide, moves.Any(m => m.Flags.HasFlag(ChessMoveFlag.KingSideCastle)), "king side");
        Assert.AreEqual(queenSide, moves.Any(m => m.Flags.HasFlag(ChessMoveFlag.QueenSideCastle)), "queen side");
    }

    #endregion

    #region DoMove

    [TestMethod]
    public void DoMove_RefusesAMoveThatLeavesYourKingInCheck()
    {
        // the bishop is pinned to the king by the rook
        var logic = FromFen("4k3/4r3/8/8/8/8/4B3/4K3 w - -");
        var before = Snapshot(logic);

        Assert.AreEqual(ChessMoveResult.BadMoveSelfCheck, logic.DoMove(ChessColor.White, Square("e2"), Square("d3")));
        Assert.AreEqual(before, Snapshot(logic), "a refused move leaves the board as it was");

        // any other move is fine
        Assert.IsTrue(logic.DoMove(ChessColor.White, Square("e1"), Square("d1")) > ChessMoveResult.NoMoveResult);
    }

    [TestMethod]
    public void DoMove_RefusesAKingMoveOntoAnAttackedSquare()
    {
        var logic = FromFen("4k3/8/8/8/8/3r4/8/4K3 w - -");

        Assert.AreEqual(ChessMoveResult.BadMoveSelfCheck, logic.DoMove(ChessColor.White, Square("e1"), Square("d2")));
        Assert.AreEqual(ChessMoveResult.BadMoveSelfCheck, logic.DoMove(ChessColor.White, Square("e1"), Square("d1")));
        Assert.AreEqual(ChessColor.White, logic.Turn);

        Assert.IsTrue(logic.DoMove(ChessColor.White, Square("e1"), Square("f2")) > ChessMoveResult.NoMoveResult);
    }

    [TestMethod]
    public void DoMove_APromotionMakesAQueenThatKeepsThePawnsGuid()
    {
        var logic = FromFen("4k3/1P6/8/8/8/8/8/4K3 w - -");
        var pawnGuid = logic.GetPiece(Square("b7")).Guid;

        var result = logic.DoMove(ChessColor.White, Square("b7"), Square("b8"));

        Assert.IsTrue(result.HasFlag(ChessMoveResult.OKMovePromotion));
        Assert.AreEqual(ChessPieceType.Queen, logic.GetPiece(Square("b8")).Type);
        Assert.AreEqual(pawnGuid, logic.GetPiece(Square("b8")).Guid);
        Assert.IsTrue(result.HasFlag(ChessMoveResult.OKMoveCheck));
    }

    [TestMethod]
    public void DoMove_EnPassantTakesThePawnBesideIt()
    {
        var logic = FromFen("4k3/3p4/8/4P3/8/8/8/4K3 b - -");

        logic.DoMove(ChessColor.Black, Square("d7"), Square("d5"));
        var result = logic.DoMove(ChessColor.White, Square("e5"), Square("d6"));

        Assert.AreEqual(ChessMoveResult.OKMoveToOccupiedSquare, result);
        Assert.IsNull(logic.GetPiece(Square("d5")));
        Assert.AreEqual(ChessPieceType.Pawn, logic.GetPiece(Square("d6")).Type);
    }

    #endregion

    #region AI

    [TestMethod]
    public void SimpleAi_LeavesTheBoardAsIfItHadOnlyPlayedItsMove()
    {
        // black can castle both ways, capture, and promote nothing: the search makes and undoes all of it
        var logic = FromFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq -");

        ChessPieceCoord from = null,
            to = null;
        var result = logic.AsyncCalculateAiSimpleMove(new ChessAiAsyncTurnKey(), ref from, ref to);

        Assert.IsTrue(result > ChessMoveResult.NoMoveResult, result.ToString());
        Assert.AreEqual(ChessColor.White, logic.Turn);

        // the same move played on a fresh board
        var expected = FromFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq -");
        Assert.IsTrue(expected.DoMove(ChessColor.Black, from, to) > ChessMoveResult.NoMoveResult);
        Assert.AreEqual(Snapshot(expected), Snapshot(logic));
    }

    [TestMethod]
    public void SimpleAi_ThatIsCheckmatedHasNoMoveAndItIsStillItsTurn()
    {
        // 1. e4 f6 2. d4 g5 3. Qh5#, black to move. ChessMatch.FinishAiMove reads whose turn it is to find the loser.
        var logic = FromFen("rnbqkbnr/ppppp2p/5p2/6pQ/3PP3/8/PPP2PPP/RNB1KBNR b KQkq -");
        var before = Snapshot(logic);

        ChessPieceCoord from = null,
            to = null;
        var result = logic.AsyncCalculateAiSimpleMove(new ChessAiAsyncTurnKey(), ref from, ref to);

        Assert.AreEqual(ChessMoveResult.NoMoveResult, result);
        Assert.AreEqual(ChessColor.Black, logic.Turn);
        Assert.AreEqual(before, Snapshot(logic));

        Assert.IsTrue(logic.InCheckmate(ChessColor.Black, true));
        Assert.AreEqual(ChessColor.Black, logic.Turn);
        Assert.IsFalse(logic.InCheckmate(ChessColor.White, true));
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Counts the legal move sequences of the given length
    /// </summary>
    private static long Perft(ChessLogic logic, int depth)
    {
        var color = logic.Turn;

        var moves = new List<ChessMove>();
        logic.GenerateMoves(color, moves);

        long nodes = 0;

        foreach (var move in moves)
        {
            logic.InternalMove(move);

            if (!logic.InCheck(color))
            {
                nodes += depth == 1 ? 1 : Perft(logic, depth - 1);
            }

            logic.UndoMove(1);
        }

        return nodes;
    }

    private static List<ChessMove> KingMoves(ChessLogic logic, ChessColor color)
    {
        var moves = new List<ChessMove>();
        logic.GenerateMoves(logic.GetPiece(color, ChessPieceType.King), true, moves);
        return moves;
    }

    private static ChessPieceCoord Square(string square)
    {
        return new ChessPieceCoord(square[0] - 'a', square[1] - '1');
    }

    private static ChessLogic FromFen(string fen)
    {
        var fields = fen.Split(' ');

        var logic = new ChessLogic { Board = new BasePiece[Chess.BoardSize * Chess.BoardSize] };

        var guid = 1u;
        var ranks = fields[0].Split('/');

        for (var i = 0; i < ranks.Length; i++)
        {
            var y = 7 - i;
            var x = 0;

            foreach (var c in ranks[i])
            {
                if (char.IsDigit(c))
                {
                    x += c - '0';
                    continue;
                }

                var type = char.ToLower(c) switch
                {
                    'p' => ChessPieceType.Pawn,
                    'n' => ChessPieceType.Knight,
                    'b' => ChessPieceType.Bishop,
                    'r' => ChessPieceType.Rook,
                    'q' => ChessPieceType.Queen,
                    _ => ChessPieceType.King
                };
                var color = char.IsUpper(c) ? ChessColor.White : ChessColor.Black;

                logic.AddPiece(color, type, (uint)x, (uint)y).Guid = new ObjectGuid(guid++);
                x++;
            }
        }

        logic.Turn = fields[1] == "w" ? ChessColor.White : ChessColor.Black;

        logic.Castling[(int)ChessColor.White] = CastlingFlags(fields[2], 'K', 'Q');
        logic.Castling[(int)ChessColor.Black] = CastlingFlags(fields[2], 'k', 'q');

        logic.EnPassantCoord = fields[3] == "-" ? null : Square(fields[3]);

        return logic;
    }

    private static ChessMoveFlag CastlingFlags(string field, char kingSide, char queenSide)
    {
        var flags = ChessMoveFlag.None;

        if (field.Contains(kingSide))
        {
            flags |= ChessMoveFlag.KingSideCastle;
        }

        if (field.Contains(queenSide))
        {
            flags |= ChessMoveFlag.QueenSideCastle;
        }

        return flags;
    }

    /// <summary>
    /// Everything a move can change, so two boards can be compared
    /// </summary>
    private static string Snapshot(ChessLogic logic)
    {
        var sb = new StringBuilder();

        for (var offset = 0; offset < logic.Board.Length; offset++)
        {
            var piece = logic.Board[offset];

            if (piece == null)
            {
                continue;
            }

            Assert.AreEqual(offset, piece.Coord.Offset, $"{piece.Color} {piece.Type} thinks it is on {piece.Coord}");

            sb.Append($"{piece.Coord}:{piece.Color} {piece.Type} {piece.Guid.Full}, ");
        }

        sb.Append($"turn {logic.Turn}, ");
        sb.Append($"castling {logic.Castling[0]} / {logic.Castling[1]}, ");
        sb.Append($"en passant {logic.EnPassantCoord?.ToString() ?? "-"}, ");
        sb.Append($"move {logic.Move}, half move {logic.HalfMove}, history {logic.History.Count}");

        return sb.ToString();
    }

    #endregion
}
