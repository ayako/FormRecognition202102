using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FormRecognitionConsole202102
{
    class Program
    {
        private static readonly string frEndpoint = "https://YOUR_ENDPOINT.cognitiveservices.azure.com/";
        private static readonly string frApiKey = "YOUR_API_KEY";
        private static readonly string frModelId = "YOUR_FR_CUSTOM_MODEL_ID";

        private static readonly AzureKeyCredential frCred = new AzureKeyCredential(frApiKey);


        static async Task Main(string[] args)
        {
            var client = new FormRecognizerClient(new Uri(frEndpoint), frCred);

            Console.WriteLine("Recognize (1)local file or (2)blob image ? (Type '1' or '2')");
            var type = Console.ReadLine();
            RecognizedFormCollection forms = null;

            switch (type)
            {
                case "1": // local file
                    Console.WriteLine("Type your local file path to recognize...");
                    var imgFilePath = Console.ReadLine();
                    var imgStream = File.Open(imgFilePath, FileMode.Open);
                    forms = await RecognizeFormFromLocalAsync(client, frModelId, imgStream);

                    Console.WriteLine("Got Analyzed Result...");
                    ShowRecognizedResult(forms);

                    break;

                case "2": // blob
                    Console.WriteLine("Type your blob url to recognize...");
                    var imgUrl = Console.ReadLine();
                    forms = await RecognizeFormFromUrlAsync(client, frModelId, imgUrl);

                    Console.WriteLine("Got Analyzed Result...");
                    ShowRecognizedResult(forms);

                    break;

                default:
                    Console.WriteLine("Got unexpected input.");
                    break;
            }


            Console.WriteLine("Type any key to close window...");
            Console.ReadLine();
        }

        private static async Task<RecognizedFormCollection> RecognizeFormFromLocalAsync(FormRecognizerClient frClient, string modelId, Stream imgStream)
        {
            var forms = await frClient.StartRecognizeCustomForms(modelId, imgStream).WaitForCompletionAsync();
            return forms;
        }

        private static async Task<RecognizedFormCollection> RecognizeFormFromUrlAsync(FormRecognizerClient frClient, string modelId, string imgUrl)
        {
            var forms = await frClient.StartRecognizeCustomFormsFromUri(modelId, new Uri(imgUrl)).WaitForCompletionAsync();
            return forms;            
        }

        private static void ShowRecognizedResult(RecognizedFormCollection forms)
        {
            foreach (RecognizedForm form in forms)
            {
                Console.WriteLine($"Custom Model Name: {form.FormType}");
                Console.WriteLine($"------");
                Console.WriteLine($"Fields:");
                foreach (FormField field in form.Fields.Values)
                {
                    Console.WriteLine($"'{field.Name}':");

                    if (field.LabelData != null)
                    {
                        Console.WriteLine($"    Label: '{field.LabelData.Text}'");
                    }

                    Console.WriteLine($"    Value: '{field.ValueData.Text}'");
                    Console.WriteLine($"    Confidence: '{field.Confidence}'");
                }
                Console.WriteLine($"------");
                Console.WriteLine($"Table data:");
                foreach (FormPage page in form.Pages)
                {
                    for (int i = 0; i < page.Tables.Count; i++)
                    {
                        FormTable table = page.Tables[i];
                        Console.WriteLine($"Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.");
                        foreach (FormTableCell cell in table.Cells)
                        {
                            Console.WriteLine($"    Cell ({cell.RowIndex}, {cell.ColumnIndex}) contains {(cell.IsHeader ? "header" : "text")}: '{cell.Text}'");
                        }
                    }
                }

                Console.WriteLine($"------");
                Console.WriteLine($"Recognized Result End");
            }
        }
    }
}
