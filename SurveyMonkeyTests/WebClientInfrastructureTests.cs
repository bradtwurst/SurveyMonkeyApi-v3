﻿using System.Text;
using NUnit.Framework;
using SurveyMonkey;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;

namespace SurveyMonkeyTests
{
    [TestFixture]
    public class WebClientInfrastructureTests
    {
        [Test]
        public void AuthenticationIsPassedToApi()
        {
            var client = new MockWebClient();
            client.Responses.Add(@"
                {""per_page"":50,""total"":2,""data"":[{""href"":""https:\/\/api.surveymonkey.net\/v3\/surveys\/55249163"",""id"":""55249163"",""title"":""All Question Types Test""},{""href"":""https:\/\/api.surveymonkey.net\/v3\/surveys\/84672934"",""id"":""84672934"",""title"":""Two Question Survey""}],""page"":1,""links"":{""self"":""https:\/\/api.surveymonkey.net\/v3\/surveys\/?page=1&per_page=50""}}
            ");

            var api = new SurveyMonkeyApi("TestApiKey", "TestOAuthToken", client);
            api.GetSurveyList();
            
            Assert.AreEqual("application/json", client.Requests.First().Headers["Content-Type"]);
            Assert.AreEqual("bearer TestOAuthToken", client.Requests.First().Headers["Authorization"]);
            Assert.AreEqual("TestApiKey", client.Requests.First().QueryString["api_key"]);
            Assert.AreEqual(Encoding.UTF8, client.Requests.First().Encoding);
            Assert.AreEqual("https://api.surveymonkey.net/v3/surveys", client.Requests.First().Url);
        }

        [Test]
        public void RateLimitIsRespected()
        {
            var toleranceMilliseconds = 80;
            var defaultRateLimitMilliseconds = 500;

            var stopwatch = new Stopwatch();
            var client = new MockWebClient();
            var repeatedResponse = @"
                {""per_page"":50,""total"":2,""data"":[{""href"":""https:\/\/api.surveymonkey.net\/v3\/surveys\/55249163"",""id"":""55249163"",""title"":""All Question Types Test""},{""href"":""https:\/\/api.surveymonkey.net\/v3\/surveys\/84672934"",""id"":""84672934"",""title"":""Two Question Survey""}],""page"":1,""links"":{""self"":""https:\/\/api.surveymonkey.net\/v3\/surveys\/?page=1&per_page=50""}}
            ";
            client.Responses.Add(repeatedResponse);
            client.Responses.Add(repeatedResponse);
            client.Responses.Add(repeatedResponse);
            client.Responses.Add(repeatedResponse);

            var api = new SurveyMonkeyApi("key", "token", client, 500);

            //Should be no rate limit first time
            stopwatch.Start();
            api.GetSurveyList();
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, toleranceMilliseconds);

            //Second request should be rate limited
            stopwatch.Restart();
            api.GetSurveyList();
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, defaultRateLimitMilliseconds - toleranceMilliseconds);
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, defaultRateLimitMilliseconds + toleranceMilliseconds);
            
            //Check subsequent requests are rate limited too
            stopwatch.Restart();
            api.GetSurveyList();
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, defaultRateLimitMilliseconds - toleranceMilliseconds);
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, defaultRateLimitMilliseconds + toleranceMilliseconds);

            //If some time has elapsed, there should be no delay
            Thread.Sleep(1000);
            stopwatch.Restart();
            api.GetSurveyList();
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, toleranceMilliseconds);
        }

        [Test]
        public void RetrySequenceIsRespected()
        {
            var toleranceMilliseconds = 80;

            var response = @"{""id"":""1234"",""name"":""Test Group"",""member_count"":1,""max_invites"":100,""date_created"":""2015-10-06T12:56:55+00:00""}";
            var client = new MockWebClient();
            client.Responses.Add(response);
            client.Responses.Add(response);
            client.Responses.Add(response);
            client.Exceptions.Add(new WebException("1"));
            client.Exceptions.Add(new WebException("2"));
            client.Exceptions.Add(new WebException("3"));
            var api = new SurveyMonkeyApi("key", "token", client, new [] { 1, 2 });

            var exception = Assert.Throws<WebException>(delegate { api.GetGroupDetails(1234); });
            Assert.AreEqual("3", exception.Message);
            Assert.AreEqual(3, client.Requests.Count);

            Assert.GreaterOrEqual(toleranceMilliseconds, client.Requests.First().TimeSinceInitialisation);

            Assert.LessOrEqual(1000 - toleranceMilliseconds, client.Requests.Skip(1).First().TimeSinceInitialisation);
            Assert.GreaterOrEqual(1000 + toleranceMilliseconds, client.Requests.Skip(1).First().TimeSinceInitialisation);

            Assert.LessOrEqual(3000 - toleranceMilliseconds, client.Requests.Skip(2).First().TimeSinceInitialisation);
            Assert.GreaterOrEqual(3000 + toleranceMilliseconds, client.Requests.Skip(2).First().TimeSinceInitialisation);
        }
    }
}