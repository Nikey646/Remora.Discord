//
//  ChannelParser.cs
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

using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Parsers;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Core;
using Remora.Results;

namespace Remora.Discord.Commands.Parsers
{
    /// <summary>
    /// Parses instances of <see cref="IChannel"/> from command-line inputs.
    /// </summary>
    public class ChannelParser : AbstractTypeParser<IChannel>
    {
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelParser"/> class.
        /// </summary>
        /// <param name="channelAPI">The channel API.</param>
        public ChannelParser(IDiscordRestChannelAPI channelAPI)
        {
            _channelAPI = channelAPI;
        }

        /// <inheritdoc />
        public override async ValueTask<RetrieveEntityResult<IChannel>> TryParse(string value, CancellationToken ct)
        {
            if (!Snowflake.TryParse(value.Unmention(), out var channelID))
            {
                return RetrieveEntityResult<IChannel>.FromError($"Failed to parse \"{value}\" as a channel ID.");
            }

            var getEntity = await _channelAPI.GetChannelAsync(channelID.Value, ct);

            return !getEntity.IsSuccess
                ? RetrieveEntityResult<IChannel>.FromError(getEntity)
                : RetrieveEntityResult<IChannel>.FromSuccess(getEntity.Entity);
        }
    }
}
