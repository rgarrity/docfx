// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Exceptions;
using Microsoft.DocAsCode.HtmlToPdf;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode;

internal static class RunPdf
{
    public static void Exec(PdfJsonConfig config, BuildOptions buildOptions, string configDirectory, string outputDirectory = null)
    {
        EnvironmentContext.SetBaseDirectory(Path.GetFullPath(string.IsNullOrEmpty(configDirectory) ? Directory.GetCurrentDirectory() : configDirectory));
        // TODO: remove BaseDirectory from Config, it may cause potential issue when abused
        var baseDirectory = EnvironmentContext.BaseDirectory;

        var wkhtmltopdfFilePath = config.Wkhtmltopdf?.FilePath is null ? null : Path.Combine(baseDirectory, config.Wkhtmltopdf.FilePath);
        ConvertWrapper.PrerequisiteCheck(wkhtmltopdfFilePath);

        if (config.Templates == null || config.Templates.Count == 0)
        {
            config.Templates = new ListWithStringFallback(new List<string> { "pdf.default" });
        }

        var outputFolder = Path.GetFullPath(Path.Combine(string.IsNullOrEmpty(outputDirectory) ? baseDirectory : outputDirectory, config.Destination ?? string.Empty));
        var rawOutputFolder = string.IsNullOrEmpty(config.RawOutputFolder) ? Path.Combine(outputFolder, "_raw") : config.RawOutputFolder;
        var options = new PdfOptions
        {
            BasePath = config.BasePath,
            CssFilePath = config.CssFilePath,
            DestDirectory = outputFolder,
            Host = config.Host,
            Locale = config.Locale,
            NeedGeneratePdfExternalLink = config.GeneratesExternalLink,
            GenerateAppendices = config.GeneratesAppendices,
            PdfConvertParallelism = config.MaxParallelism == null || config.MaxParallelism <= 0 ? Environment.ProcessorCount : config.MaxParallelism.Value,
            PdfDocsetName = config.Name ?? Path.GetFileName(EnvironmentContext.BaseDirectory),
            SourceDirectory = Path.Combine(rawOutputFolder, config.Destination ?? string.Empty),
            ExcludeTocs = config.ExcludedTocs?.ToArray(),
            KeepRawFiles = config.KeepRawFiles,
            ExcludeDefaultToc = config.ExcludeDefaultToc,
            LoadErrorHandling = config.LoadErrorHandling,
            FilePath = wkhtmltopdfFilePath,
            AdditionalPdfCommandArgs = config.Wkhtmltopdf?.AdditionalArguments,
            TocTitle = config.TocTitle,
            OutlineOption = config.OutlineOption,
            CoverPageTitle = config.CoverPageTitle,
            NoInputStreamArgs = config.NoInputStreamArgs,
        };

        // 1. call BuildCommand to generate html files first
        // Output build command exec result to temp folder
        RunBuild.Exec(config, buildOptions, configDirectory, rawOutputFolder);

        // 2. call html2pdf converter
        var converter = new ConvertWrapper(options);
        try
        {
            using (new LoggerPhaseScope("PDF", LogLevel.Info))
            {
                Logger.LogInfo("Start generating PDF files...");
                converter.Convert();
            }
        }
        catch (IOException ioe)
        {
            throw new DocfxException(ioe.Message, ioe);
        }

        // 3. Should we delete generated files according to manifest
    }
}
