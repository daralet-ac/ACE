import { PromiseExecutor } from '@nx/devkit';
import { ExecutorContext } from '@nx/devkit';
import { DotNetClient, dotnetFactory } from '@nx-dotnet/dotnet';
import {
  getExecutedProjectConfiguration,
  readInstalledDotnetToolVersion,
} from '@nx-dotnet/utils';
import { FormatExecutorSchema } from './schema';

function ensureCSharpierInstalled(dotnetClient: DotNetClient) {
  if (!readInstalledDotnetToolVersion('csharpier')) {
    dotnetClient.installTool('csharpier');
  }

  dotnetClient.restoreTools();
}

const runExecutor: PromiseExecutor<FormatExecutorSchema> = async (
  options: FormatExecutorSchema,
  context: ExecutorContext,
  dotnetClient: DotNetClient = new DotNetClient(dotnetFactory())
) => {
  const projectConfiguration = getExecutedProjectConfiguration(context);

  ensureCSharpierInstalled(dotnetClient);

  const normalizedOptions: Record<string, string | boolean> =
    Object.fromEntries(
      Object.entries(options).map(([k, v]) => [k.toLowerCase(), v])
    );

  dotnetClient.runTool(
    'dotnet-csharpier',
    [projectConfiguration.root],
    normalizedOptions
  );

  return {
    success: true,
  };
};

export default runExecutor;
