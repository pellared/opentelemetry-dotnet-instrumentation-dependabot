// <copyright file="PropertyFetcher.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
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

// From https://github.com/dotnet/runtime/blob/master/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal class PropertyFetcher
{
    private readonly string _propertyName;
    private Type _expectedType;
    private object _fetchForExpectedType;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyFetcher"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property that this instance will fetch.</param>
    public PropertyFetcher(string propertyName)
    {
        _propertyName = propertyName;
    }

    /// <summary>
    /// Gets the value of the property on the specified object.
    /// </summary>
    /// <typeparam name="T">Type of the result.</typeparam>
    /// <param name="obj">The object that contains the property.</param>
    /// <returns>The value of the property on the specified object.</returns>
    public T Fetch<T>(object obj)
    {
        return Fetch<T>(obj, obj.GetType());
    }

    /// <summary>
    /// Gets the value of the property on the specified object.
    /// </summary>
    /// <typeparam name="T">Type of the result.</typeparam>
    /// <param name="obj">The object that contains the property.</param>
    /// <param name="objType">Type of the object</param>
    /// <returns>The value of the property on the specified object.</returns>
    public T Fetch<T>(object obj, Type objType)
    {
        if (objType != _expectedType)
        {
            var propertyInfo = objType.GetProperty(_propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            _fetchForExpectedType = PropertyFetch<T>.FetcherForProperty(propertyInfo);
            _expectedType = objType;
        }

        return ((PropertyFetch<T>)_fetchForExpectedType).Fetch(obj);
    }

    /// <summary>
    /// PropertyFetch is a helper class. It takes a PropertyInfo and then knows how
    /// to efficiently fetch that property from a .NET object (See Fetch method).
    /// It hides some slightly complex generic code.
    /// </summary>
    /// <typeparam name="T">Return type of the property.</typeparam>
    private class PropertyFetch<T>
    {
        private readonly Func<object, T> _propertyFetch;

        private PropertyFetch()
        {
            _propertyFetch = _ => default;
        }

        private PropertyFetch(PropertyInfo propertyInfo)
        {
            // Generate lambda: arg => (T)((TObject)arg).get_property();
            var param = Expression.Parameter(typeof(object), "arg"); // arg =>
            var cast = Expression.Convert(param, propertyInfo.DeclaringType); // (TObject)arg
            var propertyFetch = Expression.Property(cast, propertyInfo); // get_property()
            var castResult = Expression.Convert(propertyFetch, typeof(T)); // (T)result

            // Generate the actual lambda
            var lambda = Expression.Lambda(typeof(Func<object, T>), castResult, param);

            // Compile it for faster access
            _propertyFetch = (Func<object, T>)lambda.Compile();
        }

        /// <summary>
        /// Create a property fetcher from a .NET Reflection <see cref="PropertyInfo"/> class that
        /// represents a property of a particular type.
        /// </summary>
        /// <param name="propertyInfo">The property that this instance will fetch.</param>
        /// <returns>The new property fetcher.</returns>
        public static PropertyFetch<T> FetcherForProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                // returns null on any fetch.
                return new PropertyFetch<T>();
            }

            return new PropertyFetch<T>(propertyInfo);
        }

        /// <summary>
        /// Gets the value of the property on the specified object.
        /// </summary>
        /// <param name="obj">The object that contains the property.</param>
        /// <returns>The value of the property on the specified object.</returns>
        public T Fetch(object obj)
        {
            return _propertyFetch(obj);
        }
    }
}
