using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Flurl;
using Flurl.Http;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GoshBot.Jobs
{
    [DisallowConcurrentExecution]
    public class SendPostJob : IJob
    {
        private readonly ILogger<SendPostJob> _logger;

        public SendPostJob(ILogger<SendPostJob> logger)
        {
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Start execute");

            CancellationToken cancellationToken = new CancellationToken();
            string botToken = string.Empty;
            string chatId = string.Empty;

            chatId = Environment.GetEnvironmentVariable("GOSHBOTCHATID");

            botToken = Environment.GetEnvironmentVariable("GOSHBOTTOKEN");

            _logger.LogInformation($"ChatId [{chatId}]");
            _logger.LogInformation($"BotToken [{botToken}]");

            string telegramQueryUrl = $"https://api.telegram.org/bot{botToken}";

            if(!String.IsNullOrEmpty(botToken) && !(string.IsNullOrEmpty(chatId)))
            {
                string randomRouteId = string.Empty;
                var taskGetRoutes = Task.Run(() =>
                {
                    return getRoutes();
                }
                );

                taskGetRoutes.ContinueWith((queryResult) =>
                {
                    Route route = queryResult.Result;
                    _logger.LogInformation("get route point by route:" + route.RouteId);
                    var taskGetPoints = Task.Run(() => { return getRoutePoint(route); });
                    taskGetPoints.ContinueWith((queryPointResult) =>
                    {
                        RoutePoint point = queryPointResult.Result;
                        _logger.LogInformation("get medias by point:" + point.RoutePointId);
                        var taskGetMedias = Task.Run(() => { return getMedias(point); });
                        taskGetMedias.ContinueWith((queryMedias) =>
                        {
                            List<string> imageUrl = queryMedias.Result;
                            JArray medias = new JArray();
                            string captionText = string.Empty;
                            foreach (var url in imageUrl)
                            {
                                JObject mediaObject = new JObject();
                                mediaObject.Add("type", "photo");
                                if (string.IsNullOrEmpty(captionText))
                                {
                                    captionText = $"<i>{@"" + point.Description + @"" + Environment.NewLine + Environment.NewLine  }</i>Маршрут <a href='http://igosh.pro/gallery/{ route.PublicReferenceHash }'>{ route.Name}</a>{Environment.NewLine}<i>{ @"" + route.Description + Environment.NewLine + @"" }</i>";
                                    mediaObject.Add("caption", captionText);
                                }
                                mediaObject.Add("media", url);
                                mediaObject.Add("parse_mode", "HTML");
                                medias.Add(mediaObject);
                            }

                            _logger.LogInformation("request sendMediaGroup");
                            var taskSendMediaGroup = Task.Run(async () => await telegramQueryUrl.AppendPathSegment("sendMediaGroup").SetQueryParam("chat_id", chatId).SetQueryParam("media", medias.ToString()).PostAsync(), cancellationToken);
                            taskSendMediaGroup.ContinueWith((status) =>
                            {
                                _logger.LogInformation("Telegram API result code:" + status.Result.StatusCode);
                            });
                        });
                    });
                });
            }
                

            return Task.CompletedTask;
        }

        private async Task<Route> getRoutes()
        {
            var routeRequest = "http://igosh.pro/api/v2/public/routes".SetQueryParam("pageSize", "1000").SetQueryParam("range", "[0,9]");
            List<Route> resultListRoutes = new List<Route>();
            try
            {
                resultListRoutes = await routeRequest.GetJsonAsync<List<Route>>();      
                _logger.LogInformation($"Route requested, count [{ resultListRoutes.Count }]");
            }
            catch (FlurlHttpException ex)
            {
                _logger.LogInformation("Route requested. Flurl http error:" + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Route requested. Error:" + ex.Message);
            }
            Random rnd = new Random();
            int index = rnd.Next(0, resultListRoutes.Count);
            return resultListRoutes[index];
        }

        private async Task<RoutePoint> getRoutePoint(Route route)
        {
            string jsonRouteId = @"{""routeId"":""" + route.RouteId + @"""}";
            var routeRequest = "http://igosh.pro/api/v2/public/RoutePoints".SetQueryParam("pageSize", "1000").SetQueryParam("range", "[0,9]").SetQueryParam("filter", jsonRouteId);
            List<RoutePoint> resultListPoints = new List<RoutePoint>();
            try
            {
                resultListPoints = await routeRequest.GetJsonAsync<List<RoutePoint>>();      
                _logger.LogInformation($"route point requested, count [{ resultListPoints.Count }]");
            }
            catch (FlurlHttpException ex)
            {
                _logger.LogInformation("Route point requested. Flurl http error:" + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("route point requested. Error:" + ex.Message);
            }
            Random rnd = new Random();
            int index = rnd.Next(0, resultListPoints.Count);
            return resultListPoints[index];
        }

        private async Task<List<string>> getMedias(RoutePoint routePoint)
        {
            var routeRequest = "http://igosh.pro/api/v2/public/RoutePoints".AppendPathSegment(routePoint.RoutePointId).AppendPathSegment("medias");
            List<String> resultListUrls = new List<string>();
            try
            {
                resultListUrls = await routeRequest.GetJsonAsync<List<String>>();      
                _logger.LogInformation($"Medias requested, count [{ resultListUrls.Count }]");
            }
            catch (FlurlHttpException ex)
            {
                _logger.LogInformation("Medias requested. Flurl http error:" + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Medias requested. Error:" + ex.Message);
            }
            return resultListUrls;
        }

    }

    internal class RoutePoint
    {
        public string RoutePointId;
        public string Description;
    }

    internal class Route
    {
        public string RouteId;
        public string Name;
        public string Description;
        public string PublicReferenceHash;
    }
}
