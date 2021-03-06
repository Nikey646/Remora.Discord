//
//  InteractionResponder.cs
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
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Services;
using Remora.Commands.Trees;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Discord.Gateway.Results;

namespace Remora.Discord.Commands.Responders
{
    /// <summary>
    /// Responds to interactions.
    /// </summary>
    public class InteractionResponder : IResponder<IInteractionCreate>
    {
        private readonly CommandService _commandService;
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly ExecutionEventCollectorService _eventCollector;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionResponder"/> class.
        /// </summary>
        /// <param name="commandService">The command service.</param>
        /// <param name="interactionAPI">The interaction API.</param>
        /// <param name="eventCollector">The event collector.</param>
        /// <param name="services">The available services.</param>
        public InteractionResponder
        (
            CommandService commandService,
            IDiscordRestInteractionAPI interactionAPI,
            ExecutionEventCollectorService eventCollector,
            IServiceProvider services
        )
        {
            _commandService = commandService;
            _eventCollector = eventCollector;
            _services = services;
            _interactionAPI = interactionAPI;
        }

        /// <inheritdoc />
        public async Task<EventResponseResult> RespondAsync
        (
            IInteractionCreate? gatewayEvent,
            CancellationToken ct = default
        )
        {
            if (gatewayEvent is null)
            {
                return EventResponseResult.FromSuccess();
            }

            if (!gatewayEvent.Data.HasValue)
            {
                return EventResponseResult.FromSuccess();
            }

            if (!gatewayEvent.Member.User.HasValue)
            {
                return EventResponseResult.FromSuccess();
            }

            // Signal Discord that we'll be handling this one asynchronously
            var response = new InteractionResponse(InteractionResponseType.Acknowledge, default);
            var interactionResponse = await _interactionAPI.CreateInteractionResponseAsync
            (
                gatewayEvent.ID,
                gatewayEvent.Token,
                response,
                ct
            );

            if (!interactionResponse.IsSuccess)
            {
                return EventResponseResult.FromError(interactionResponse);
            }

            var interactionData = gatewayEvent.Data.Value!;
            interactionData.UnpackInteraction(out var command, out var parameters);

            var context = new InteractionContext
            (
                gatewayEvent.ChannelID,
                gatewayEvent.Member.User.Value!,
                gatewayEvent.Member,
                gatewayEvent.Token,
                gatewayEvent.ID,
                gatewayEvent.GuildID
            );

            // Run any user-provided pre execution events
            var preExecution = await _eventCollector.RunPreExecutionEvents(context, ct);
            if (!preExecution.IsSuccess)
            {
                return EventResponseResult.FromError(preExecution);
            }

            // Run the actual command
            var searchOptions = new TreeSearchOptions(StringComparison.OrdinalIgnoreCase);
            var executeResult = await _commandService.TryExecuteAsync
            (
                command,
                parameters,
                _services,
                new object[] { context },
                searchOptions,
                ct
            );

            if (!executeResult.IsSuccess)
            {
                return EventResponseResult.FromError(executeResult);
            }

            // Run any user-provided post execution events
            var postExecution = await _eventCollector.RunPostExecutionEvents
            (
                context,
                executeResult.InnerResult!,
                ct
            );

            if (!postExecution.IsSuccess)
            {
                return EventResponseResult.FromError(postExecution);
            }

            return EventResponseResult.FromSuccess();
        }
    }
}
