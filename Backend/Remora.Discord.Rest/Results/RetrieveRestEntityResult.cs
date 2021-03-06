//
//  RetrieveRestEntityResult.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Results;

namespace Remora.Discord.Rest.Results
{
    /// <summary>
    /// Represents an attempt to create an entity via the REST API.
    /// </summary>
    /// <typeparam name="TEntity">The entity to create.</typeparam>
    [PublicAPI]
    public class RetrieveRestEntityResult<TEntity> :
        AbstractRestResult<RetrieveRestEntityResult<TEntity>>,
        IRetrieveRestEntityResult<TEntity>
    {
        /// <summary>
        /// Holds the actual entity value.
        /// </summary>
        [MaybeNull, AllowNull]
        private readonly TEntity _entity = default!;

        /// <inheritdoc />
        public TEntity Entity
        {
            [return: MaybeNull]
            get
            {
                if (!this.IsSuccess)
                {
                    throw new InvalidOperationException("The result does not contain a valid value.");
                }

                return _entity!;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetrieveRestEntityResult{TEntity}"/> class.
        /// </summary>
        /// <param name="entity">The entity.</param>
        private RetrieveRestEntityResult(TEntity entity)
        {
            _entity = entity;
        }

        /// <inheritdoc cref="RetrieveRestEntityResult{TResultType}"/>
        [UsedImplicitly]
        private RetrieveRestEntityResult
        (
            string? errorReason,
            Exception? exception = null
        )
            : base(errorReason, exception)
        {
        }

        /// <inheritdoc cref="RetrieveRestEntityResult{TResultType}"/>
        [UsedImplicitly]
        private RetrieveRestEntityResult
        (
            string? errorReason,
            IRestError? discordError = null
        )
            : base(errorReason, discordError)
        {
        }

        /// <inheritdoc cref="RetrieveRestEntityResult{TResultType}"/>
        [UsedImplicitly]
        private RetrieveRestEntityResult
        (
            string? errorReason,
            HttpStatusCode? statusCode = null
        )
            : base(errorReason, statusCode)
        {
        }

        /// <summary>
        /// Creates a new successful result.
        /// </summary>
        /// <param name="entity">The entity that was retrieved.</param>
        /// <returns>A successful result.</returns>
        public static RetrieveRestEntityResult<TEntity> FromSuccess(TEntity entity)
        {
            return new RetrieveRestEntityResult<TEntity>(entity);
        }

        /// <summary>
        /// Implicitly converts a compatible value to a successful result.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The successful result.</returns>
        public static implicit operator RetrieveRestEntityResult<TEntity>(TEntity entity)
        {
            return FromSuccess(entity);
        }
    }
}
