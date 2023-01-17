using CovidCertificate.Backend.Models.Deserializers;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.EndpointValidation;

namespace CovidCertificate.Backend.Configuration.Bases.ValidationService
{
    public class PostEndpointValidationService : IPostEndpointValidationService
    {
        protected readonly ILogger<PostEndpointValidationService> logger;

        public PostEndpointValidationService(ILogger<PostEndpointValidationService> logger)
        {
            this.logger = logger;
        }

        public async Task<IActionResult> ValidatePostWithFhirPayloadAsync<T, VT>(HttpRequest req) where T : Base where VT : AbstractValidator<T>, new()
        {
            logger.LogTraceAndDebug("ValidatePostWithFhirPayloadAsync was invoked");

            T dto;
            try
            {
                dto = FHIRDeserializer.Deserialize<T>(await ExtractPayloadAsync(req));
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return new BadRequestObjectResult("Failed to parse fhir request body");
            }

            var validator = new VT();

            var validResult = validator.Validate(dto);
            logger.LogTraceAndDebug($"validResult: IsValid is {validResult?.IsValid}");

            if (!validResult.IsValid)
            {
                logger.LogTraceAndDebug("ValidatePostWithFhirPayloadAsync has finished");
                return new BadRequestObjectResult(validResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                }));
            }

            logger.LogTraceAndDebug("ValidatePostWithFhirPayloadAsync has finished");
            return new OkObjectResult(dto);
        }

        public async Task<IActionResult> ValidatePostAsync<T, VT>(HttpRequest req) where VT : AbstractValidator<T>, new()
        {
            logger.LogInformation("ValidatePostAsync was invoked");

            T dto;
            try
            {
                dto = JsonConvert.DeserializeObject<T>(await ExtractPayloadAsync(req));
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return new BadRequestObjectResult("Failed to parse request body");
            }

            var validator = new VT();

            var validResult = validator.Validate(dto);
            logger.LogTraceAndDebug($"validResult: IsValid is {validResult?.IsValid}");

            if (!validResult.IsValid)
            {
                logger.LogInformation("ValidatePostAsync has finished");
                return new BadRequestObjectResult(validResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                }));
            }

            logger.LogInformation("ValidatePostAsync has finished");
            return new OkObjectResult(dto);
        }

        public async Task<T> CastRequestToObjectAsync<T>(HttpRequest request)
        {
            if (request is null)
                throw new ArgumentNullException($"Failed to initialize a new instance of type {typeof(T)} as the specified request was null.");
            else if (request.Body is null)
                throw new ArgumentNullException($"Failed to initialize a new instance of type {typeof(T)} as the specified request body stream was null.");
            else if (!request.Body.CanRead)
                throw new ArgumentException($"Failed to initialize a new instance of type {typeof(T)} as the specified request body stream" +
                    $" either contains characters larger than {nameof(int.MaxValue)}, have already been disposed or is currently in use by another read operation.");
            else if (request.Body.Length is 0)
                throw new ArgumentException($"Failed to initialize a new instance of type {typeof(T)} as the specified request body stream was empty.");

            using var reader = new StreamReader(request.Body);
            var requestBodyString = await reader.ReadToEndAsync();

            try
            {
                return JsonConvert.DeserializeObject<T>(requestBodyString) ?? 
                    throw new NullReferenceException($"No properties was populated during deserialization or the JSON constructor was inaccessible for the request body: {requestBodyString}");
            }
            catch (JsonSerializationException e)
            {
                throw new ArgumentException($"Failed to parse request body: {requestBodyString}", e);
            }
        }

        private async Task<string> ExtractPayloadAsync(HttpRequest request)
        {
            string requestBodyString = "";
            using (var reader = new StreamReader(request.Body))
            {
                requestBodyString = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(requestBodyString))
            {
                logger.LogInformation("ValidatePost has finished");
                throw new Exception("No request body present");
            }

            return requestBodyString;
        }
    }
}
