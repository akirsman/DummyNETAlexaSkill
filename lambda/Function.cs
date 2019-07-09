using System;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using AlexaAPI;
using AlexaAPI.Request;
using AlexaAPI.Response;
using System.Text.RegularExpressions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace sampleFactCsharp
{
    public class Function
    {
        private SkillResponse _response = null;
        private ILambdaContext _context = null;
        const string LOCALENAME = "locale";
        const string ESMX_Locale = "es-MX";

        /// <summary>
        /// Application entry point
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext ctx)
        {
            _context = ctx;
            Log("Input object:" + JsonConvert.SerializeObject(input));
            try
            {
                _response = new SkillResponse();
                _response.Response = new ResponseBody();
                _response.Response.ShouldEndSession = false;
                _response.Version = AlexaConstants.AlexaVersion;

                if (input.Request.Type.Equals(AlexaConstants.LaunchRequest))
                {
                    string locale = input.Request.Locale;
                    if (string.IsNullOrEmpty(locale))
                    {
                        locale = ESMX_Locale;
                    }

                    ProcessLaunchRequest(_response.Response);
                    _response.SessionAttributes = new Dictionary<string, object>() {{LOCALENAME, locale}};
                }
                else
                {
                    if (input.Request.Type.Equals(AlexaConstants.IntentRequest))
                    {
                       _response.SessionAttributes = new Dictionary<string, object>() {{LOCALENAME, ESMX_Locale}};
                       if (IsDialogIntentRequest(input))
                       {
                            if (!IsDialogSequenceComplete(input))
                            { // delegate to Alexa until dialog is complete
                                CreateDelegateResponse();
                                return _response;
                            }
                       }

                       if (!ProcessDialogRequest(input, _response))
                       {
                           _response.Response.OutputSpeech = ProcessIntentRequest(input);
                       }
                    }
                }
                Log(JsonConvert.SerializeObject(_response));
                return _response;
            }
            catch (Exception ex)
            {
                Log($"error :" + ex.Message);
            }
            return null; 
        }

        /// <summary>
        /// Process and respond to the LaunchRequest with launch
        /// and reprompt message
        /// </summary>
        /// <param name="factdata"></param>
        /// <param name="response"></param>
        /// <returns>void</returns>
        private void ProcessLaunchRequest(ResponseBody response)
        {
            IOutputSpeech innerResponse = new SsmlOutputSpeech();
            (innerResponse as SsmlOutputSpeech).Ssml = SsmlDecorate("Bienvenido al ejemplo de skill de animales");
            response.OutputSpeech = innerResponse;
            IOutputSpeech prompt = new PlainTextOutputSpeech();
            (prompt as PlainTextOutputSpeech).Text = "Bienvenido otra vez al ejemplo de skill de animales";
            response.Reprompt = new Reprompt()
            {
                OutputSpeech = prompt
            };
        }

        /// <summary>
        /// Check if its IsDialogIntentRequest, e.g. part of a Dialog sequence
        /// </summary>
        /// <param name="input"></param>
        /// <returns>bool true if a dialog</returns>
        private bool IsDialogIntentRequest(SkillRequest input)
        {
            if (string.IsNullOrEmpty(input.Request.DialogState))
                return false;
            return true;
        }

        /// <summary>
        /// Check if its Dialog sequence is complete
        /// </summary>
        /// <param name="input"></param>
        /// <returns>bool true if dialog complete set</returns>
        private bool IsDialogSequenceComplete(SkillRequest input)
        {
            if (input.Request.DialogState.Equals(AlexaConstants.DialogStarted)
               || input.Request.DialogState.Equals(AlexaConstants.DialogInProgress))
            { 
                return false ;
            }
            else
            {
                if (input.Request.DialogState.Equals(AlexaConstants.DialogCompleted))
                {
                    return true;
                }
            }
            return false;
        }

        // <summary>
        ///  Process intents that are dialog based and may not have a speech
        ///  response. Speech responses cannot be returned with a delegate response
        /// </summary>
        /// <param name="factdata"></param>
        /// <param name="input"></param>
        /// <param name="response"></param>
        /// <returns>bool true if processed</returns>
        private bool ProcessDialogRequest(SkillRequest input, SkillResponse response)
        {
            var intentRequest = input.Request;
            string speech_message = string.Empty;
            bool processed = false;

            switch (intentRequest.Intent.Name)
            {
                case "AnimalFactIntent":
                    speech_message = GetAnimalFact(intentRequest);
                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;
            }

            return processed;
        }

        /// <summary>
        ///  Get aninmal fact
        /// </summary>
        /// <param name="factdata"></param>
        /// <param name="request"></param>
        /// <returns>animal fact speech</returns>
        private string GetAnimalFact(Request request)
        {
            string speech_message = string.Empty;
            if (request.Intent.Slots.ContainsKey("animal"))
            {
                Slot slot = null;
                if (request.Intent.Slots.TryGetValue("animal", out slot))
                {
                    if (slot.Value != null)
                    {
                        speech_message = "Ejemplo de dato sobre " + slot.Value.ToLower();
                    }
                    else
                    {
                        speech_message = "No se entendió sobre qué animal se preguntó";
                    }
                }
            }
            return speech_message;
        }

        /// <summary>
        ///  prepare text for Ssml display
        /// </summary>
        /// <param name="speech"></param>
        /// <returns>string</returns>
        private string SsmlDecorate(string speech)
        {
            return "<speak>" + speech + "</speak>";
        }

        /// <summary>
        /// Process all not dialog based Intents
        /// </summary>
        /// <param name="factdata"></param>
        /// <param name="input"></param>
        /// <returns>IOutputSpeech innerResponse</returns>
        private IOutputSpeech ProcessIntentRequest(SkillRequest input)
        {
            var intentRequest = input.Request;
            IOutputSpeech innerResponse = new PlainTextOutputSpeech();
            
            switch (intentRequest.Intent.Name)
            {
                case "AnimalFactIntent":
                    innerResponse = new SsmlOutputSpeech();
                    (innerResponse as SsmlOutputSpeech).Ssml = $"los {intentRequest.Intent.Slots["animal"].Value} son buenos";
                    break;
                case AlexaConstants.CancelIntent:
                    (innerResponse as PlainTextOutputSpeech).Text = "cancelando";
                    _response.Response.ShouldEndSession = true;
                    break;

                case AlexaConstants.StopIntent:
                    (innerResponse as PlainTextOutputSpeech).Text = "parando";
                    _response.Response.ShouldEndSession = true;                    
                    break;

                case AlexaConstants.HelpIntent:
                    (innerResponse as PlainTextOutputSpeech).Text = "Este es un ejemplo de skill de Alexa que nos cuenta datos de animales"; 
                    break;

                default:
                    (innerResponse as PlainTextOutputSpeech).Text = "Este es un ejemplo de skill de Alexa que nos cuenta datos de animales"; 
                    break;
            }
            if (innerResponse.Type == AlexaConstants.SSMLSpeech)
            {
                BuildCard("Animal", (innerResponse as SsmlOutputSpeech).Ssml);
                (innerResponse as SsmlOutputSpeech).Ssml = SsmlDecorate((innerResponse as SsmlOutputSpeech).Ssml);
            }  
            return innerResponse;
        }

        /// <summary>
        /// Build a simple card, setting its title and content field 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <returns>void</returns>
        private void BuildCard(string title, string output)
        {
            if (!string.IsNullOrEmpty(output))
            {                
                output = Regex.Replace(output, @"<.*?>", "");
                _response.Response.Card = new SimpleCard()
                {
                    Title = title,
                    Content = output,
                };  
            }
        }               

        /// <summary>
        ///  create a delegate response, we delegate all the dialog requests
        ///  except "Complete"
        /// </summary>
        /// <returns>void</returns>
        private void CreateDelegateResponse()
        {
            DialogDirective dld = new DialogDirective()
            {
                Type = AlexaConstants.DialogDelegate
            };
            _response.Response.Directives.Add(dld);
        }

        /// <summary>
        /// logger interface
        /// </summary>
        /// <param name="text"></param>
        /// <returns>void</returns>
        private void Log(string text)
        {
            if (_context != null)
            {
                _context.Logger.LogLine(text);
            }
        }
    }
}
