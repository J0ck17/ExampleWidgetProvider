using Microsoft.Windows.Widgets.Providers;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ExampleWidgetProvider
{
    // WidgetProvider.cs
    internal class WidgetProvider : IWidgetProvider
    {
        // Class member of WidgetProvider
        public static Dictionary<string, CompactWidgetInfo> RunningWidgets = new Dictionary<string, CompactWidgetInfo>();

        static ManualResetEvent emptyWidgetListEvent = new ManualResetEvent(false);

        public static ManualResetEvent GetEmptyWidgetListEvent()
        {
            return emptyWidgetListEvent;
        }
        private static int writeCounter = 0;
        const string countWidgetTemplate = @"
{                                                                     
    ""type"": ""AdaptiveCard"",                                         
    ""body"": [  
        {
            ""type"": ""Input.Text"",
            ""id"": ""inputTextId"",
            ""placeholder"": ""Saisissez ici..."",
            ""inlineAction"": {
                ""type"": ""Action.Execute"",
                ""title"": ""Ecrire"",
                ""role"": ""Button"",
                ""verb"": ""writeFile""
        }
        },
        {
            ""text"": ""${dynamicContent}"",
            ""type"": ""TextBlock""            
        },
        {                                                               
            ""type"": ""TextBlock"",                                    
            ""text"": ""You have clicked the button ${count} times""    
        },
        {
                ""text"":""Rendering Only if Small"",
                ""type"":""TextBlock"",
                ""$when"":""${$host.widgetSize==\""small\""}""
        },
        {
                ""text"":""Rendering Only if Medium"",
                ""type"":""TextBlock"",
                ""$when"":""${$host.widgetSize==\""medium\""}""
        },
        {
            ""text"":""Rendering Only if Large"",
            ""type"":""TextBlock"",
            ""$when"":""${$host.widgetSize==\""large\""}""
        }                                                                    
    ],                                                                  
    ""actions"": [                                                      
        {                                                               
            ""type"": ""Action.Execute"",                               
            ""title"": ""Increment"",                                   
            ""verb"": ""inc""                                           
        },
        {
            ""type"": ""Action.Execute"",
            ""title"": ""Lecture"",
            ""verb"": ""readFile"",
            ""style"": ""positive""
        },
        {
            ""type"": ""Action.Execute"",
            ""title"": ""Reset"",
            ""verb"": ""reset"",
            ""data"": {
                    ""confirmationMessage"": ""Êtes-vous sûr de vouloir réinitialiser ?"",
            ""style"": ""destructive""
    }
        }
    ],                                                                  
    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
    ""version"": ""1.5""                                                
}";


        public WidgetProvider()
        {
            var runningWidgets = WidgetManager.GetDefault().GetWidgetInfos();

            foreach (var widgetInfo in runningWidgets)
            {
                var widgetContext = widgetInfo.WidgetContext;
                var widgetId = widgetContext.Id;
                var widgetName = widgetContext.DefinitionId;
                var customState = widgetInfo.CustomState;
                if (!RunningWidgets.ContainsKey(widgetId))
                {
                    CompactWidgetInfo runningWidgetInfo = new CompactWidgetInfo() { widgetId = widgetName, widgetName = widgetId };
                    try
                    {
                        // If we had any save state (in this case we might have some state saved for Counting widget)
                        // convert string to required type if needed.
                        int count = Convert.ToInt32(customState.ToString());
                        runningWidgetInfo.customState = count;
                    }
                    catch
                    {

                    }
                    RunningWidgets[widgetId] = runningWidgetInfo;
                }
            }
        }

        public void CreateWidget(WidgetContext widgetContext)
        {
            var widgetId = widgetContext.Id; // To save RPC calls
            var widgetName = widgetContext.DefinitionId;
            CompactWidgetInfo runningWidgetInfo = new CompactWidgetInfo() { widgetId = widgetId, widgetName = widgetName };
            RunningWidgets[widgetId] = runningWidgetInfo;


            // Update the widget
            UpdateWidget(runningWidgetInfo);
        }

        public void DeleteWidget(string widgetId, string customState)
        {
            RunningWidgets.Remove(widgetId);

            if (RunningWidgets.Count == 0)
            {
                emptyWidgetListEvent.Set();
            }
        }

        public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
        {
            var verb = actionInvokedArgs.Verb;

            if (verb == "inc")
            {
                var widgetId = actionInvokedArgs.WidgetContext.Id;
                var data = actionInvokedArgs.Data;

                if (RunningWidgets.ContainsKey(widgetId))
                {
                    var localWidgetInfo = RunningWidgets[widgetId];
                    localWidgetInfo.customState++;
                    UpdateWidget(localWidgetInfo);
                }
            }
            else if (verb == "writeFile")
            {
                var widgetId = actionInvokedArgs.WidgetContext.Id;
                var data = actionInvokedArgs.Data;

                // Obtenir le contenu à écrire dans le fichier 
                var contentToWrite = $"Ligne {++writeCounter}: Nouveau contenu à écrire dans le fichier. Horodatage : {DateTime.Now}";

                // Écrire le contenu dans le fichier texte
                var filePath = @"C:\Temp\POC.txt";
                try
                {
                    // Vérifiez si le fichier existe, sinon le créer
                    if (!File.Exists(filePath))
                    {
                        File.CreateText(filePath).Close();
                    }

                    File.AppendAllText(filePath, contentToWrite + Environment.NewLine);

                    // On peut rajouter des actions ici si nécessaire.
                }

                catch (Exception ex)
                {
                    // Gérez l'erreur selon les besoins de l'application.
                }
            }
            else if (verb == "readFile")
            {
                var filePath = @"C:\Temp\POC.txt";

                try
                {
                    // Vérifiez si le fichier existe avant de lancer le processus de lecture
                    if (File.Exists(filePath))
                    {
                        // Ouvrir le fichier texte dans Notepad.exe
                        Process.Start("notepad.exe", filePath);
                    }
                    else
                    {
                        // Le fichier n'existe pas, gérer le code erreur.
                    }
                }
                catch (Exception ex)
                {
                    // Gérez l'erreur selon les besoins de l'application.
                }
            }
            else if (verb == "reset")
            {
                var filePath = @"C:\Temp\POC.txt";

                try
                {
                    // Réinitialiser l'incrément à zéro
                    writeCounter = 0;

                    // Effacer le contenu du fichier POC.txt
                    File.WriteAllText(filePath, string.Empty);
                }
                catch (Exception ex)
                {
                    // Gérez l'erreur selon les besoins de l'application.
                }
            }
        }

        public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
        {
            var widgetContext = contextChangedArgs.WidgetContext;
            var widgetId = widgetContext.Id;
            var widgetSize = widgetContext.Size;
            if (RunningWidgets.ContainsKey(widgetId))
            {
                var localWidgetInfo = RunningWidgets[widgetId];
                UpdateWidget(localWidgetInfo);
            }
        }

        public void Activate(WidgetContext widgetContext)
        {
            var widgetId = widgetContext.Id;

            if (RunningWidgets.ContainsKey(widgetId))
            {
                var localWidgetInfo = RunningWidgets[widgetId];
                localWidgetInfo.isActive = true;

                UpdateWidget(localWidgetInfo);
            }
        }

        public void Deactivate(string widgetId)
        {
            if (RunningWidgets.ContainsKey(widgetId))
            {
                var localWidgetInfo = RunningWidgets[widgetId];
                localWidgetInfo.isActive = false;
            }
        }

        void UpdateWidget(CompactWidgetInfo localWidgetInfo)
        {
            WidgetUpdateRequestOptions updateOptions = new WidgetUpdateRequestOptions(localWidgetInfo.widgetId);

            string templateJson = countWidgetTemplate.ToString();

            string dataJson = "{ \"count\": " + localWidgetInfo.customState.ToString() + " }";

            updateOptions.Template = templateJson;
            updateOptions.Data = dataJson;
            // You can store some custom state in the widget service that you will be able to query at any time.
            updateOptions.CustomState = localWidgetInfo.customState.ToString();
            //updateOptions.DynamicContentText = localWidgetInfo.dynamicContentText;
            WidgetManager.GetDefault().UpdateWidget(updateOptions);
        }


        public class CompactWidgetInfo
        {
            public string? widgetId { get; set; }
            public string? widgetName { get; set; }
            public int customState = 0;
            public string dynamicContentText = "";
            public bool isActive = false;

        }
    }
}

