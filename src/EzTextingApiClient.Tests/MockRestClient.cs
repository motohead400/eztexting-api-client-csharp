﻿using System;
using RestSharp;
using System.Reflection;
using NUnit.Framework;
using System.Linq;
using RestSharp.Deserializers;

namespace EzTextingApiClient.Tests
{
    public class MockRestClient : RestClient
    {
        private readonly HttpResponse _mockResponse;

        public Ref<IRestRequest> CapturedRequest { get; set; }

        public MockRestClient(IRestClient restClient, IDeserializer deserializer = null)
            : this(restClient, deserializer, null)
        {
        }

        public MockRestClient(IRestClient restClient, IDeserializer deserializer, HttpResponse response = null)
        {
            CapturedRequest = new Ref<IRestRequest>(null);
            _mockResponse = response ?? new HttpResponse();
            BaseUrl = restClient.BaseUrl;
            Authenticator = restClient.Authenticator;
            UserAgent = restClient.UserAgent;
            AddHandler("application/json", deserializer);
        }

        public override IRestResponse Execute(IRestRequest request)
        {
            CapturedRequest.Value = request;
            var method = Enum.GetName(typeof(Method), request.Method);
            return InvokePrivateExecute(request, method);
        }

        public override IRestResponse<T> Execute<T>(IRestRequest request)
        {
            return InvokePrivateDeserialize<T>(request, this.Execute(request));
        }

        public HttpResponse DoReturnMockResponse(IHttp http, string method)
        {
            _mockResponse.ContentType = "application/json";
            _mockResponse.ContentEncoding = "UTF-8";
            _mockResponse.StatusCode = _mockResponse.StatusCode == 0
                ? System.Net.HttpStatusCode.OK
                : _mockResponse.StatusCode;
            _mockResponse.ResponseStatus = ResponseStatus.Completed;
            return _mockResponse;
        }

        protected IRestResponse<T> InvokePrivateDeserialize<T>(IRestRequest request, IRestResponse raw)
        {
            var method = typeof(RestClient).GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method = method.MakeGenericMethod(typeof(T));
            var response = (IRestResponse<T>) method.Invoke(this, new object[] {request, raw});
            return response;
        }

        protected IRestResponse InvokePrivateExecute(IRestRequest request, string httpMethod)
        {
            var method = typeof(RestClient).GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var response = method.Invoke(this,
                new object[] {request, httpMethod, (Func<IHttp, string, HttpResponse>) DoReturnMockResponse});
            return (IRestResponse) response;
        }
    }

    public class Ref<T>
    {
        public T Value { get; set; }

        public Ref(T value)
        {
            Value = value;
        }
    }
}