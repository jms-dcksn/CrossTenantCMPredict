using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using UiPath.CodedWorkflows;
using UiPath.Core;
using UiPath.Core.Activities.Storage;
using UiPath.Orchestrator.Client.Models;
using UiPath.Testing;
using UiPath.Testing.Activities.TestData;
using UiPath.Testing.Activities.TestDataQueues.Enums;
using UiPath.Testing.Enums;
using UiPath.UIAutomationNext.API.Contracts;
using UiPath.UIAutomationNext.API.Models;
using UiPath.UIAutomationNext.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace CrossPlatformTestCMPredict
{
    public class CrossTenantCMPredict : CodedWorkflow
    {
        [Workflow]
        public MessagePrediction Execute(string apiKey, string from, string to, string sentAt, string subject, string message, string url)
        {
            string payload = HttpPostAsync(apiKey, from, to, sentAt, subject, message, url);
            MessagePrediction resultPrediction = JsonDeserializer.DeserializeWithSystemTextJson(payload);

            return resultPrediction;
        }
        
        public static string HttpPostAsync(string apiKey, string from, string to, string sentAt, string subject, string message, string url)
        {
            string content = null;
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri: url);
            request.Headers.Add("Authorization", "Bearer " + apiKey);


            PredictRequest predictRequest = CreatePredictRequest.CreateInstance(from, to, sentAt, subject, message);

            var body = new StringContent(JsonSerialize.SerializeToJson(predictRequest), System.Text.Encoding.UTF8, "application/json");

            request.Content = body;

            var response = client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                content =  response.Content.ReadAsStringAsync().Result;
            }

            return content;
        }
    }
    
    //*************Class objects to structure payload*************
    
    public class PredictRequest
    {
        public List<Document> Documents { get; set; }
    }

    public class Document
    {
        public List<Message> Messages { get; set; }
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("user_properties")]
        public UserProperties UserProperties { get; set; }
    }

    public class Message
    {
        public Body Body { get; set; }
        public string From { get; set; }

        [JsonPropertyName("sent_at")]
        public DateTime SentAt { get; set; }
        public Subject Subject { get; set; }
        public List<string> To { get; set; }
    }

    public class Body
    {
        public string Text { get; set; }
    }

    public class Subject
    {
        public string Text { get; set; }
    }

    public class UserProperties
    {
    }
    
    //**********Class Objects to structure response****************
    
    public class MessagePrediction
    {
        public List<List<Prediction>> Predictions { get; set; }
        public Model Model { get; set; }
        public List<List<LabelProperty>> LabelProperties { get; set; }
        public string Status { get; set; }
    }

    public class Prediction
    {
        public List<string> Name { get; set; }
        public double Probability { get; set; }
    }

    public class Model
    {
        public int Version { get; set; }
        public DateTime Time { get; set; }
    }

    public class LabelProperty
    {
        public string PropertyId { get; set; }
        public string PropertyName { get; set; }
        public double Value { get; set; }
    }
    
    //********************Helper methods*********************
    
    public class JsonDeserializer
    {
        public static MessagePrediction DeserializeWithSystemTextJson(string jsonString)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return System.Text.Json.JsonSerializer.Deserialize<MessagePrediction>(jsonString, options);
        }
    }

    public class JsonSerialize {
        public static string SerializeToJson(PredictRequest predictRequest)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = new SnakeCaseNamingPolicy()
            };
            return JsonSerializer.Serialize(predictRequest, options);

        }
    }

    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(String name)
        {
            return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }
    }

    //Helper methods for creating request payload
    public class CreatePredictRequest {
        public static PredictRequest CreateInstance(string from, string to, string sentAt, string subject, string msg)
        {
            return new PredictRequest
            {
                Documents = new List<Document>
            {
                new Document
                {
                    Messages = new List<Message>
                    {
                        new Message
                        {
                            Body = new Body
                            {
                                Text = msg
                            },
                            From = from,
                            SentAt = DateTime.Parse(sentAt),
                            Subject = new Subject
                            {
                                Text = subject
                            },
                            To = new List<string> { to }
                        }
                    },
                    Timestamp = DateTime.Now,
                    UserProperties = new UserProperties()
                }
            }
            };
        }
    }
        
}