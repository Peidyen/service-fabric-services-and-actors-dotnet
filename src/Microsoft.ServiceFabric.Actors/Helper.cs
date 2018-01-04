﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors
{
    using System;
    using System.Globalization;
    using Microsoft.ServiceFabric.Actors.Remoting;

    internal class Helper
    {
        public static string GetCallContext()
        {
            string callContextValue;
            if (ActorLogicalCallContext.TryGet(out callContextValue))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}",
                    callContextValue,
                    Guid.NewGuid().ToString());
            }

            return Guid.NewGuid().ToString();
        }
    }
}