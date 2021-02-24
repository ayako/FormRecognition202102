using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
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
                    Console.WriteLine("Type your local folder path to recognize...");
                    var imgFolderPath = Console.ReadLine();
                    var imgFiles = Directory.GetFiles(imgFolderPath);

                    foreach (var img in imgFiles)
                    {
                        var imgStream = File.Open(img, FileMode.Open);
                        forms = await RecognizeFormFromLocalAsync(client, frModelId, imgStream);

                        Console.WriteLine("Got Analyzed Result: " + Path.GetFileName(img));
                        //ShowRecognizedResult(forms);
                        var resultText = await LogRecognizedResultAsync(forms);
                        await File.WriteAllTextAsync(Path.Join(imgFolderPath, (Path.GetFileNameWithoutExtension(img) + ".txt")), resultText);
                    }

                    break;

                case "2": // blob
                    Console.WriteLine("Type your blob container url to recognize...");
                    var imgContainerPath = Console.ReadLine();
                    Console.WriteLine("Type your local folder path to save results...");
                    var resultFolderPath = Console.ReadLine();

                    var imgContainer = new BlobContainerClient(new Uri(imgContainerPath));
                    var imgBlobs = imgContainer.GetBlobsAsync();

                    await foreach (var imgBlobItem in imgBlobs)
                    {
                        var imBlob = imgContainer.GetBlockBlobClient(imgBlobItem.Name).Uri.ToString();
                        forms = await RecognizeFormFromUrlAsync(client, frModelId, imBlob);

                        Console.WriteLine("Got Analyzed Result: " + imgBlobItem.Name);
                        //ShowRecognizedResult(forms);
                        var resultText = await LogRecognizedResultAsync(forms);

                        await File.WriteAllTextAsync(Path.Join(resultFolderPath, (Path.GetFileNameWithoutExtension(imgBlobItem.Name) + ".txt")), resultText);
                    }

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

        //private static void ShowRecognizedResult(RecognizedFormCollection forms)
        //{
        //    foreach (RecognizedForm form in forms)
        //    {
        //        Console.WriteLine($"Custom Model Name: {form.FormType}");
        //        Console.WriteLine($"------");
        //        Console.WriteLine($"Fields:");
        //        foreach (FormField field in form.Fields.Values)
        //        {
        //            Console.WriteLine($"'{field.Name}':");

        //            if (field.LabelData != null)
        //            {
        //                Console.WriteLine($"    Label: '{field.LabelData.Text}'");
        //            }

        //            Console.WriteLine($"    Value: '{field.ValueData.Text}'");
        //            Console.WriteLine($"    Confidence: '{field.Confidence}'");
        //        }
        //        Console.WriteLine($"------");
        //        Console.WriteLine($"Table data:");
        //        foreach (FormPage page in form.Pages)
        //        {
        //            for (int i = 0; i < page.Tables.Count; i++)
        //            {
        //                FormTable table = page.Tables[i];
        //                Console.WriteLine($"Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.");
        //                foreach (FormTableCell cell in table.Cells)
        //                {
        //                    Console.WriteLine($"    Cell ({cell.RowIndex}, {cell.ColumnIndex}) contains {(cell.IsHeader ? "header" : "text")}: '{cell.Text}'");
        //                }
        //            }
        //        }

        //        Console.WriteLine($"------");
        //        Console.WriteLine($"Recognized Result End");
        //    }
        //}

        private static async Task<string> LogRecognizedResultAsync(RecognizedFormCollection forms)
        {
            string resultText = null;

            foreach (RecognizedForm form in forms)
            {
                resultText += $"Custom Model Name: {form.FormType}\n";
                resultText += $"------\nFields:\n";
                foreach (FormField field in form.Fields.Values)
                {
                    resultText += $"'{field.Name}':\n";

                    if (field.LabelData != null)
                    {
                        resultText += $"    Label: '{field.LabelData.Text}'\n";
                    }

                    resultText += $"    Value: '{field.ValueData.Text}'\n";
                    resultText += $"    Confidence: '{field.Confidence}'\n";
                }
                resultText += $"------\nTable data:\n";
                foreach (FormPage page in form.Pages)
                {
                    for (int i = 0; i < page.Tables.Count; i++)
                    {
                        FormTable table = page.Tables[i];
                        resultText += $"Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.\n";
                        foreach (FormTableCell cell in table.Cells)
                        {
                            resultText += $"    Cell ({cell.RowIndex}, {cell.ColumnIndex}) contains {(cell.IsHeader ? "header" : "text")}: '{cell.Text}'\n";
                        }
                    }
                }

                resultText += $"------\nRecognized Result End";
            }

            return resultText;
        }

    }
}
