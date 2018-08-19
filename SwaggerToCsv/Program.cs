using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using SwaggerToCsv.Models;

namespace SwaggerToCsv
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CommandLineApplication.ExecuteAsync<Program>(args);
        }

        [Required]
        [Option(CommandOptionType.SingleValue, Description = "Swagger JSON file path", LongName = "input",
            ShortName = "i")]
        public string InputFilePath { get; set; }

        [Required]
        [Option(CommandOptionType.SingleValue, Description = "Output CSV file path", LongName = "output",
            ShortName = "o")]
        public string OutputFilePath { get; set; }


        private async Task<int> OnExecuteAsync()
        {
            // File validation
            if (string.IsNullOrWhiteSpace(InputFilePath) || !File.Exists(InputFilePath))
            {
                Console.Error.WriteLine("Input file \"{0}\" not found.", InputFilePath);
                return 1;
            }

            var (loadFileResult, data) = await LoadFileAsync();
            if (!loadFileResult)
            {
                return 1;
            }

            var csvModels = data.Paths
                .SelectMany(path => path.Value.Select(method => new CsvModel
                {
                    Method = method.Key.ToUpper(),
                    Summary = method.Value.Summary,
                    Url = path.Key
                }))
                .OrderBy(m => m.Url)
                .ToList();

            var writeResult = await WriteCsvAsync(csvModels);
            return writeResult ? 0 : 1;
        }

        private async Task<(bool Success, SwaggerRoot Data)> LoadFileAsync()
        {
            try
            {
                var text = await File.ReadAllTextAsync(InputFilePath, Encoding.UTF8);
                var data = JsonConvert.DeserializeObject<SwaggerRoot>(text);
                return (true, data);
            }
            catch (JsonException e)
            {
                Console.Error.WriteLine("JSON parse error. " + e.Message);
                return (false, null);
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("JSON IO error. " + e.Message);
                return (false, null);
            }
        }

        private async Task<bool> WriteCsvAsync(IEnumerable<CsvModel> models)
        {
            try
            {
                using (var writer = new CsvWriter(File.CreateText(OutputFilePath)))
                {
                    writer.Configuration.HasHeaderRecord = true;
                    writer.WriteRecords(models);
                    await writer.FlushAsync();

                    return true;
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("CSV write error. " + e.Message);
                return false;
            }
        }
    }
}