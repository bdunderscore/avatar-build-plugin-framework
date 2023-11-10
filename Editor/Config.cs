﻿/*
 * MIT License
 *
 * Copyright (c) 2022 bd_
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#region

using System;
using UnityEditor;
using UnityEngine;

#endregion

namespace nadena.dev.ndmf.config
{
    internal class NonPersistentConfig : ScriptableSingleton<NonPersistentConfig>
    {
        [SerializeField] public bool applyOnPlay = true;
    }


    public static class Config
    {
        /// <summary>
        /// Controls whether NDMF transformations will be applied at play time.
        /// </summary>
        public static bool ApplyOnPlay
        {
            get => NonPersistentConfig.instance.applyOnPlay;
            set
            {
                NonPersistentConfig.instance.applyOnPlay = value;
                NotifyChange();
            }
        }

        /// <summary>
        /// This event will be invoked when any config value changes.
        /// </summary>
        public static event Action OnChange;

        private static void NotifyChange() => OnChange?.Invoke();
    }
}