//
//  DiscordHttpClient.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
using Remora.Discord.Rest.API;
using Remora.Discord.Rest.Results;

namespace Remora.Discord.Rest
{
    /// <summary>
    /// Represents a specialized HTTP client for the Discord API.
    /// </summary>
    [PublicAPI]
    public class DiscordHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _serializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscordHttpClient"/> class.
        /// </summary>
        /// <param name="httpClient">The Http client.</param>
        /// <param name="serializerOptions">The serialization options.</param>
        public DiscordHttpClient
        (
            HttpClient httpClient,
            IOptions<JsonSerializerOptions> serializerOptions
        )
        {
            _httpClient = httpClient;
            _serializerOptions = serializerOptions.Value;
        }

        /// <summary>
        /// Performs a GET request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="allowNullReturn">Whether to allow null return values inside the creation result.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <typeparam name="TEntity">The entity type to retrieve.</typeparam>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<IRetrieveRestEntityResult<TEntity>> GetAsync<TEntity>
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            bool allowNullReturn = false,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Get);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                return await UnpackResponseAsync<TEntity>(response, allowNullReturn, ct);
            }
            catch (Exception e)
            {
                return RetrieveRestEntityResult<TEntity>.FromError(e);
            }
        }

        /// <summary>
        /// Performs a GET request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<IRetrieveRestEntityResult<Stream>> GetContentAsync
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Get);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                var unpackedResponse = await UnpackResponseAsync(response, ct);
                if (!unpackedResponse.IsSuccess)
                {
                    return RetrieveRestEntityResult<Stream>.FromError(unpackedResponse);
                }

                var responseContent = await response.Content.ReadAsStreamAsync();
                return RetrieveRestEntityResult<Stream>.FromSuccess(responseContent);
            }
            catch (Exception e)
            {
                return RetrieveRestEntityResult<Stream>.FromError(e);
            }
        }

        /// <summary>
        /// Performs a POST request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="allowNullReturn">Whether to allow null return values inside the creation result.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <typeparam name="TEntity">The entity type to create.</typeparam>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<ICreateRestEntityResult<TEntity>> PostAsync<TEntity>
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            bool allowNullReturn = false,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Post);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                var entityResult = await UnpackResponseAsync<TEntity>(response, allowNullReturn, ct);
                return !entityResult.IsSuccess
                    ? CreateRestEntityResult<TEntity>.FromError(entityResult)
                    : CreateRestEntityResult<TEntity>.FromSuccess(entityResult.Entity);
            }
            catch (Exception e)
            {
                return CreateRestEntityResult<TEntity>.FromError(e);
            }
        }

        /// <summary>
        /// Performs a POST request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A post result which may or may not have succeeded.</returns>
        public async Task<IRestResult> PostAsync
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Post);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                var entityResult = await UnpackResponseAsync(response, ct);
                return !entityResult.IsSuccess
                    ? RestRequestResult.FromError(entityResult)
                    : RestRequestResult.FromSuccess();
            }
            catch (Exception e)
            {
                return RestRequestResult.FromError(e);
            }
        }

        /// <summary>
        /// Performs a PATCH request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="allowNullReturn">Whether to allow null return values inside the creation result.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <typeparam name="TEntity">The entity type to modify.</typeparam>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<IModifyRestEntityResult<TEntity>> PatchAsync<TEntity>
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            bool allowNullReturn = false,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Patch);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                var entityResult = await UnpackResponseAsync<TEntity>(response, allowNullReturn, ct);
                return !entityResult.IsSuccess
                    ? ModifyRestEntityResult<TEntity>.FromError(entityResult)
                    : ModifyRestEntityResult<TEntity>.FromSuccess(entityResult.Entity);
            }
            catch (Exception e)
            {
                return ModifyRestEntityResult<TEntity>.FromError(e);
            }
        }

        /// <summary>
        /// Performs a PATCH request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<IRestResult> PatchAsync
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Patch);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                var entityResult = await UnpackResponseAsync(response, ct);
                return !entityResult.IsSuccess
                    ? RestRequestResult.FromError(entityResult)
                    : RestRequestResult.FromSuccess();
            }
            catch (Exception e)
            {
                return RestRequestResult.FromError(e);
            }
        }

        /// <summary>
        /// Performs a DELETE request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<IDeleteRestEntityResult> DeleteAsync
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Delete);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                var entityResult = await UnpackResponseAsync(response, ct);
                return !entityResult.IsSuccess
                    ? DeleteRestEntityResult.FromError(entityResult)
                    : DeleteRestEntityResult.FromSuccess();
            }
            catch (Exception e)
            {
                return DeleteRestEntityResult.FromError(e);
            }
        }

        /// <summary>
        /// Performs a DELETE request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="allowNullReturn">Whether to allow null return values inside the creation result.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <typeparam name="TEntity">The type of entity to create.</typeparam>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<IDeleteRestEntityResult<TEntity>> DeleteAsync<TEntity>
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            bool allowNullReturn = false,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Delete);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                var entityResult = await UnpackResponseAsync<TEntity>(response, allowNullReturn, ct);
                return !entityResult.IsSuccess
                    ? DeleteRestEntityResult<TEntity>.FromError(entityResult)
                    : DeleteRestEntityResult<TEntity>.FromSuccess(entityResult.Entity);
            }
            catch (Exception e)
            {
                return DeleteRestEntityResult<TEntity>.FromError(e);
            }
        }

        /// <summary>
        /// Performs a PUT request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="allowNullReturn">Whether to allow null return values inside the creation result.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <typeparam name="TEntity">The type of entity to create.</typeparam>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<ICreateRestEntityResult<TEntity>> PutAsync<TEntity>
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            bool allowNullReturn = false,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Put);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                var entityResult = await UnpackResponseAsync<TEntity>(response, allowNullReturn, ct);
                return !entityResult.IsSuccess
                    ? CreateRestEntityResult<TEntity>.FromError(entityResult)
                    : CreateRestEntityResult<TEntity>.FromSuccess(entityResult.Entity);
            }
            catch (Exception e)
            {
                return CreateRestEntityResult<TEntity>.FromError(e);
            }
        }

        /// <summary>
        /// Performs a PUT request to the Discord REST API at the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="configureRequestBuilder">The request builder for the request.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<IRestResult> PutAsync
        (
            string endpoint,
            Action<RestRequestBuilder>? configureRequestBuilder = null,
            CancellationToken ct = default
        )
        {
            configureRequestBuilder ??= _ => { };

            var requestBuilder = new RestRequestBuilder(endpoint);
            configureRequestBuilder(requestBuilder);

            requestBuilder.WithMethod(HttpMethod.Put);

            try
            {
                using var request = requestBuilder.Build();
                using var response = await _httpClient.SendAsync
                (
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                var entityResult = await UnpackResponseAsync(response, ct);
                return !entityResult.IsSuccess
                    ? RestRequestResult.FromError(entityResult)
                    : RestRequestResult.FromSuccess();
            }
            catch (Exception e)
            {
                return RestRequestResult.FromError(e);
            }
        }

        /// <summary>
        /// Unpacks a response from the Discord API, attempting to either a plain success or a parsed
        /// error.
        /// </summary>
        /// <param name="response">The response to unpack.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        private async Task<IRestResult> UnpackResponseAsync
        (
            HttpResponseMessage response,
            CancellationToken ct = default
        )
        {
            if (response.IsSuccessStatusCode)
            {
                return RestRequestResult.FromSuccess();
            }

            // See if we have a JSON error to get some more details from
            var reasonPhrase = response.ReasonPhrase ?? "Unknown error.";
            if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength <= 0)
            {
                return RestRequestResult.FromError
                (
                    reasonPhrase,
                    response.StatusCode
                );
            }

            try
            {
                var jsonError = await JsonSerializer.DeserializeAsync<RestError>
                (
                    await response.Content.ReadAsStreamAsync(),
                    _serializerOptions,
                    ct
                );

                if (jsonError is null)
                {
                    return RestRequestResult.FromError
                    (
                        reasonPhrase,
                        response.StatusCode
                    );
                }

                return RestRequestResult.FromError
                (
                    jsonError.Message,
                    jsonError
                );
            }
            catch
            {
                return RestRequestResult.FromError
                (
                    reasonPhrase,
                    response.StatusCode
                );
            }
        }

        /// <summary>
        /// Unpacks a response from the Discord API, attempting to either get the requested entity type or a parsed
        /// error.
        /// </summary>
        /// <param name="response">The response to unpack.</param>
        /// <param name="allowNullReturn">Whether to allow null return values inside the creation result.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <typeparam name="TEntity">The entity type to unpack.</typeparam>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        private async Task<IRetrieveRestEntityResult<TEntity>> UnpackResponseAsync<TEntity>
        (
            HttpResponseMessage response,
            bool allowNullReturn = false,
            CancellationToken ct = default
        )
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentLength == 0)
                {
                    if (!allowNullReturn)
                    {
                        throw new InvalidOperationException("Response content null, but null returns not allowed.");
                    }

                    // Null is okay as a default here, since TEntity might be TEntity?
                    return RetrieveRestEntityResult<TEntity>.FromSuccess(default!);
                }

                var entity = await JsonSerializer.DeserializeAsync<TEntity>
                (
                    await response.Content.ReadAsStreamAsync(),
                    _serializerOptions,
                    ct
                );

                if (entity is not null)
                {
                    return RetrieveRestEntityResult<TEntity>.FromSuccess(entity);
                }

                if (!allowNullReturn)
                {
                    throw new InvalidOperationException("Response content null, but null returns not allowed.");
                }

                // Null is okay as a default here, since TEntity might be TEntity?
                return RetrieveRestEntityResult<TEntity>.FromSuccess(default!);
            }

            // See if we have a JSON error to get some more details from
            var reasonPhrase = response.ReasonPhrase ?? "Unknown error.";
            if (!response.Content.Headers.ContentLength.HasValue || !(response.Content.Headers.ContentLength > 0))
            {
                return RetrieveRestEntityResult<TEntity>.FromError
                (
                    reasonPhrase,
                    response.StatusCode
                );
            }

            try
            {
                var jsonError = await JsonSerializer.DeserializeAsync<RestError>
                (
                    await response.Content.ReadAsStreamAsync(),
                    _serializerOptions,
                    ct
                );

                if (jsonError is null)
                {
                    return RetrieveRestEntityResult<TEntity>.FromError
                    (
                        reasonPhrase,
                        response.StatusCode
                    );
                }

                return RetrieveRestEntityResult<TEntity>.FromError
                (
                    jsonError.Message,
                    jsonError
                );
            }
            catch
            {
                return RetrieveRestEntityResult<TEntity>.FromError
                (
                    reasonPhrase,
                    response.StatusCode
                );
            }
        }
    }
}
