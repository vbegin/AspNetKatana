﻿// <copyright file="DefaultFilesMiddleware.cs" company="Microsoft Open Technologies, Inc.">
// Copyright 2011-2013 Microsoft Open Technologies, Inc. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;

namespace Microsoft.Owin.StaticFiles
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// This examines a directory path and determines if there is a default file present.
    /// If so the file name is appended to the path and execution continues.
    /// Note we don't just serve the file because it may require interpretation.
    /// </summary>
    public class DefaultFilesMiddleware
    {
        private readonly DefaultFilesOptions _options;
        private readonly AppFunc _next;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "By design")]
        public DefaultFilesMiddleware(AppFunc next, DefaultFilesOptions options)
        {
            _options = options;
            _next = next;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public Task Invoke(IDictionary<string, object> environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }

            string subpath;
            IDirectoryInfo directory;
            string defaultFile;
            if (Helpers.IsGetOrHeadMethod(environment)
                && Helpers.PathEndsInSlash(environment) // The DirectoryBrowser will redirect for missing slashes.
                && Helpers.TryMatchPath(environment, _options.RequestPath, forDirectory: true, subpath: out subpath)
                && TryGetDirectoryInfo(subpath, out directory)
                && TryGetDefaultFile(directory, out defaultFile))
            {
                environment[Constants.RequestPathKey] = (string)environment[Constants.RequestPathKey] + defaultFile;
            }

            return _next(environment);
        }

        private bool TryGetDirectoryInfo(string subpath, out IDirectoryInfo directory)
        {
            return _options.FileSystem.TryGetDirectoryInfo(subpath, out directory);
        }

        private bool TryGetDefaultFile(IDirectoryInfo directory, out string defaultFile)
        {
            // DefaultFileNames are prioritized so we have to search in this order.
            IList<IFileInfo> files = directory.GetFiles().ToList();
            for (int matchIndex = 0; matchIndex < _options.DefaultFileNames.Count; matchIndex++)
            {
                string matchFile = _options.DefaultFileNames[matchIndex];

                for (int fileIndex = 0; fileIndex < files.Count; fileIndex++)
                {
                    if (files[fileIndex].Name.Equals(matchFile, StringComparison.OrdinalIgnoreCase))
                    {
                        defaultFile = matchFile;
                        return true;
                    }
                }
            }

            defaultFile = null;
            return false;
        }
    }
}
