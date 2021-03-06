// <copyright file="ValidateIntegration.cs" company="OpenTelemetry Authors">
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

using System;
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL;

/// <summary>
/// GraphQL.Validation.DocumentValidator calltarget instrumentation
/// </summary>
[InstrumentMethod(
    AssemblyName = GraphQLCommon.GraphQLAssembly,
    TypeName = "GraphQL.Validation.DocumentValidator",
    MethodName = "Validate",
    ReturnTypeName = "GraphQL.Validation.IValidationResult",
    ParameterTypeNames = new[] { ClrNames.String, "GraphQL.Types.ISchema", "GraphQL.Language.AST.Document", "System.Collections.Generic.IEnumerable`1[GraphQL.Validation.IValidationRule]", ClrNames.Ignore, "GraphQL.Inputs" },
    MinimumVersion = GraphQLCommon.Major2Minor3,
    MaximumVersion = GraphQLCommon.Major2,
    IntegrationName = GraphQLCommon.IntegrationName)]
public class ValidateIntegration
{
    private const string ErrorType = "GraphQL.Validation.ValidationError";

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TSchema">Type of the schema</typeparam>
    /// <typeparam name="TDocument">Type of the document</typeparam>
    /// <typeparam name="TRules">Type of the rules</typeparam>
    /// <typeparam name="TUserContext">Type of the user context</typeparam>
    /// <typeparam name="TInputs">Type of the inputs</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="originalQuery">The source of the original GraphQL query</param>
    /// <param name="schema">The GraphQL schema value</param>
    /// <param name="document">The GraphQL document value</param>
    /// <param name="rules">The list of validation rules</param>
    /// <param name="userContext">The user context</param>
    /// <param name="inputs">The input variables</param>
    /// <returns>Calltarget state value</returns>
    public static CallTargetState OnMethodBegin<TTarget, TSchema, TDocument, TRules, TUserContext, TInputs>(TTarget instance, string originalQuery, TSchema schema, TDocument document, TRules rules, TUserContext userContext, TInputs inputs)
        where TDocument : IDocument
    {
        return new CallTargetState(GraphQLCommon.CreateActivityFromValidate(document));
    }

    /// <summary>
    /// OnMethodEnd callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TValidationResult">Type of the validation result value</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="validationResult">IValidationResult instance</param>
    /// <param name="exception">Exception instance in case the original code threw an exception.</param>
    /// <param name="state">Calltarget state value</param>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    public static CallTargetReturn<TValidationResult> OnMethodEnd<TTarget, TValidationResult>(TTarget instance, TValidationResult validationResult, Exception exception, CallTargetState state)
        where TValidationResult : IValidationResult
    {
        Activity activity = state.Activity;
        if (activity is null)
        {
            return new CallTargetReturn<TValidationResult>(validationResult);
        }

        try
        {
            if (exception != null)
            {
                activity.SetException(exception);
            }
            else
            {
                GraphQLCommon.RecordExecutionErrorsIfPresent(activity, ErrorType, validationResult.Errors);
            }
        }
        finally
        {
            activity.Dispose();
        }

        return new CallTargetReturn<TValidationResult>(validationResult);
    }
}
