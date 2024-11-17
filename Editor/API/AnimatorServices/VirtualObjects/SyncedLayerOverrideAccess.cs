#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using HarmonyLib;
using UnityEditor.Animations;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     The AnimatorControllerLayer class does not provide efficient bulk access to its internal m_Motions and
    ///     m_Behaviours lists, which would make introspecting a synced layer quite slow. This class constructs some
    ///     JITtable accessors to help us get at those lists in bulk.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class SyncedLayerOverrideAccess
    {
        public static readonly Func<AnimatorControllerLayer, IEnumerable<KeyValuePair<AnimatorState, Motion>>?>
            ExtractStateMotionPairs;

        public static readonly Action<AnimatorControllerLayer, IEnumerable<KeyValuePair<AnimatorState, Motion>>>
            SetStateMotionPairs;

        public static readonly
            Func<AnimatorControllerLayer, IEnumerable<KeyValuePair<AnimatorState, ScriptableObject[]>>?>
            ExtractStateBehaviourPairs;

        public static readonly
            Action<AnimatorControllerLayer, IEnumerable<KeyValuePair<AnimatorState, ScriptableObject[]>>>
            SetStateBehaviourPairs;

        static SyncedLayerOverrideAccess()
        {
            ExtractStateMotionPairs =
                Generate_ExtractStateMotionPairs<AnimatorState, Motion>("m_Motions", "m_State", "m_Motion");
            SetStateMotionPairs = Generate_Setter<AnimatorState, Motion>("m_Motions", "m_State", "m_Motion");

            ExtractStateBehaviourPairs =
                Generate_ExtractStateMotionPairs<AnimatorState, ScriptableObject[]>("m_Behaviours", "m_State",
                    "m_Behaviours");
            SetStateBehaviourPairs =
                Generate_Setter<AnimatorState, ScriptableObject[]>("m_Behaviours", "m_State", "m_Behaviours");
        }

        private static Action<AnimatorControllerLayer, IEnumerable<KeyValuePair<K, V>>> Generate_Setter<K, V>(
            string fieldName,
            string keyField,
            string valueField
        )
        {
            var arrayField = AccessTools.Field(typeof(AnimatorControllerLayer), fieldName);
            var t_Pair_arr = arrayField.FieldType;
            var t_Pair = t_Pair_arr.GetElementType();

            var f_pair_key = AccessTools.Field(t_Pair, keyField);
            var f_pair_value = AccessTools.Field(t_Pair, valueField);

            var var_item = Expression.Variable(t_Pair, "item");
            var ex_key = Expression.Field(var_item, f_pair_key);
            var ex_value = Expression.Field(var_item, f_pair_value);

            var p_kvp = Expression.Parameter(typeof(KeyValuePair<K, V>), "kvp");

            var construct_and_set = Expression.Block(
                new[] { var_item },
                // item = new StateMotionPair()
                Expression.Assign(var_item, Expression.New(t_Pair)),
                // item.m_State = kvp.Key
                Expression.Assign(ex_key, Expression.PropertyOrField(p_kvp, "Key")),
                // item.m_Motion = kvp.Value
                Expression.Assign(ex_value, Expression.PropertyOrField(p_kvp, "Value")),
                // return item
                var_item
            );
            var kvp_to_pair_ty = typeof(Func<,>).MakeGenericType(typeof(KeyValuePair<K, V>), t_Pair);
            var kvp_to_pair = Expression.Lambda(kvp_to_pair_ty, construct_and_set, p_kvp);

            var m_toArray = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(t_Pair);
            var m_select = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Length == 2
                                               && m.GetParameters()[1].ParameterType.GenericTypeArguments.Length == 2
                );
            var m_select_t = m_select.MakeGenericMethod(typeof(KeyValuePair<K, V>), t_Pair);

            var p_layer = Expression.Parameter(typeof(AnimatorControllerLayer), "layer");
            var p_pairs = Expression.Parameter(typeof(IEnumerable<KeyValuePair<K, V>>), "pairs");

            // pairs => layer.m_Motions = pairs.Select(kvp => new StateMotionPair {m_State = kvp.Key, m_Motion = kvp.Value}).ToArray()
            var ex_select = Expression.Call(m_select_t, p_pairs, kvp_to_pair);
            var ex_toArray = Expression.Call(m_toArray, ex_select);
            var ex_assign = Expression.Assign(Expression.Field(p_layer, arrayField), ex_toArray);
            var lambda = Expression.Lambda<
                Action<AnimatorControllerLayer, IEnumerable<KeyValuePair<K, V>>>
            >(ex_assign, p_layer, p_pairs);

            return lambda.Compile();
        }

        private static Func<AnimatorControllerLayer, IEnumerable<KeyValuePair<K, V>>>
            Generate_ExtractStateMotionPairs<K, V>(
                string fieldName,
                string keyField,
                string valueField
            )
        {
            var arrayField = AccessTools.Field(typeof(AnimatorControllerLayer), fieldName);
            var t_Pair_arr = arrayField.FieldType;
            var t_Pair = t_Pair_arr.GetElementType();

            var f_pair_key = AccessTools.Field(t_Pair, keyField);
            var f_pair_value = AccessTools.Field(t_Pair, valueField);

            var p_item = Expression.Parameter(t_Pair, "item");
            var ex_key = Expression.Field(p_item, f_pair_key);
            var ex_value = Expression.Field(p_item, f_pair_value);

            var ctor_kvp = typeof(KeyValuePair<K, V>).GetConstructor(new[] { typeof(K), typeof(V) });
            var ex_build_kvp = Expression.New(ctor_kvp, ex_key, ex_value);

            // Func<StateMotionPair, KeyValuePair<AnimatorState, Motion>>
            var lambda_type = typeof(Func<,>).MakeGenericType(t_Pair, typeof(KeyValuePair<K, V>));
            var lambda_convert = Expression.Lambda(lambda_type, ex_build_kvp, p_item);

            // Now build a lambda which will convert the m_Motions array, using the Linq Select method
            var p_layer = Expression.Parameter(typeof(AnimatorControllerLayer), "layer");
            Expression ex_field_array = Expression.Field(p_layer, arrayField);

            // Add a null check and fallback for the array
            var enum_empty = typeof(Enumerable).GetMethod("Empty")!.MakeGenericMethod(t_Pair).Invoke(null, null);
            var ex_null_check = Expression.Condition(
                Expression.Equal(ex_field_array, Expression.Constant(null)),
                Expression.Constant(enum_empty),
                ex_field_array
            );

            var m_select = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Length == 2
                                               && m.GetParameters()[1].ParameterType.GenericTypeArguments.Length == 2
                );
            var m_select_t = m_select.MakeGenericMethod(t_Pair, typeof(KeyValuePair<K, V>));
            var ex_select = Expression.Call(m_select_t, ex_null_check, lambda_convert);

            return Expression.Lambda<
                    Func<AnimatorControllerLayer, IEnumerable<KeyValuePair<K, V>>>
                >(ex_select, p_layer)
                .Compile();
        }
    }
}