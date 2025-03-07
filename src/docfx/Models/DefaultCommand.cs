// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using Microsoft.DocAsCode.Dotnet;
using Newtonsoft.Json;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

class DefaultCommand : Command<DefaultCommand.Options>
{
    [Description("Runs metadata, build and pdf commands")]
    internal class Options : BuildCommandOptions
    {
        [Description("Prints version information")]
        [CommandOption("-v|--version")]
        public bool Version { get;set; }
    }

    public override int Execute(CommandContext context, Options options)
    {
        if (options.Version)
        {
            Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            return 0;
        }

        return CommandHelper.Run(options, () =>
        {
            var (config, baseDirectory) = CommandHelper.GetConfig<Config>(options.ConfigFile);
            var outputFolder = options.OutputFolder;
            string serveDirectory = null;

            if (config.Metadata is not null)
                DotnetApiCatalog.Exec(config.Metadata, new(), baseDirectory, outputFolder).GetAwaiter().GetResult();

            if (config.Build is not null)
            {
                BuildCommand.MergeOptionsToConfig(options, config.Build, baseDirectory);
                serveDirectory = RunBuild.Exec(config.Build, new(), baseDirectory, outputFolder);
            }

            if (config.Pdf is not null)
            {
                BuildCommand.MergeOptionsToConfig(options, config.Pdf, baseDirectory);
                RunPdf.Exec(config.Pdf, new(), baseDirectory, outputFolder);
            }

            if (options.Serve && serveDirectory is not null)
            {
                RunServe.Exec(serveDirectory, options.Host, options.Port);
            }
        });
    }

    class Config
    {
        [JsonProperty("build")]
        public BuildJsonConfig Build { get; set; }

        [JsonProperty("metadata")]
        public MetadataJsonConfig Metadata { get; set; }

        [JsonProperty("pdf")]
        public PdfJsonConfig Pdf { get; set; }
    }
}
