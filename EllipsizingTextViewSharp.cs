﻿/*
 * Copyright (C) 2011 Micah Hainline
 * Copyright (C) 2012 Triposo
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Widget;
using Java.Lang;
using Java.Util.Regex;
using String = System.String;

namespace com.kobynet.text
{
    public class EllipsizingTextViewSharp : TextView
    {
        private const string ELLIPSIS = "\u2026";
        private static readonly Pattern DefaultEndPunctuation = Pattern.Compile("[\\.,\u2026;\\:\\s]*$", RegexOptions.Dotall);

        public interface IEllipsizeListener
        {
            void EllipsizeStateChanged(bool ellipsized);
        }

        private readonly List<IEllipsizeListener> _ellipsizeListeners = new List<IEllipsizeListener>();
        private bool _isEllipsized;
        private bool _isStale;
        private bool _programmaticChange;
        private string _fullText;
        private float _lineSpacingMultiplier = 1.0f;
        private float _lineAdditionalVerticalPadding;
        private int _lineDecender;
        /**
        * The end punctuation which will be removed when appending #ELLIPSIS.
        */
        private Pattern _endPunctuationPattern;

        protected EllipsizingTextViewSharp(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Init();
        }

        public EllipsizingTextViewSharp(Context context)
            : base(context)
        {
            Init();
        }

        public EllipsizingTextViewSharp(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init();
        }

        public EllipsizingTextViewSharp(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Init();
        }

        private void Init()
        {
            base.Ellipsize = null;

            SetMaxLines(int.MaxValue);

            SetEndPunctuationPattern(DefaultEndPunctuation);
        }

        public void SetEndPunctuationPattern(Pattern pattern)
        {
            _endPunctuationPattern = pattern;
        }

        public void AddEllipsizeListener(IEllipsizeListener listener)
        {
            if (listener == null)
            {
                throw new NullPointerException();
            }
            _ellipsizeListeners.Add(listener);
        }

        public void RemoveEllipsizeListener(IEllipsizeListener listener)
        {
            _ellipsizeListeners.Remove(listener);
        }

        public bool IsEllipsized()
        {
            return _isEllipsized;
        }

        public override void SetMaxLines(int maxlines)
        {
            base.SetMaxLines(maxlines);
            MaxLines = maxlines;
            _isStale = true;
        }

        protected int MaxLines { get; private set; }

        public bool EllipsizingLastFullyVisibleLine()
        {
            return MaxLines == Integer.MaxValue;
        }

        public override void SetLineSpacing(float add, float mult)
        {
            _lineAdditionalVerticalPadding = add;
            _lineSpacingMultiplier = mult;
            base.SetLineSpacing(add, mult);
        }

        protected override void OnTextChanged(ICharSequence text, int start, int before, int after)
        {
            base.OnTextChanged(text, start, before, after);
            if (_programmaticChange == false)
            {
                _fullText = text.ToString();
                _isStale = true;
            }
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            if (EllipsizingLastFullyVisibleLine())
            {
                _isStale = true;
            }
        }

        public override void SetPadding(int left, int top, int right, int bottom)
        {
            base.SetPadding(left, top, right, bottom);
            if (EllipsizingLastFullyVisibleLine())
            {
                _isStale = true;
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            if (_isStale)
            {
                ResetText();
            }
            base.OnDraw(canvas);
        }

        public override void SetTextSize(ComplexUnitType unit, float size)
        {
            base.SetTextSize(unit, size);
            var fontMetrics = Paint.GetFontMetricsInt();
            _lineDecender = fontMetrics.Descent;
            if (EllipsizingLastFullyVisibleLine())
            {
                _isStale = true;
            }
        }

        private void ResetText()
        {
            var workingText = _fullText;
            var ellipsized = false;
            var layout = CreateWorkingLayout(workingText);
            var linesCount = GetLinesCount();
            if (layout.LineCount > linesCount)
            {
                // We have more lines of text than we are allowed to display.
                workingText = _fullText.Substring(0, layout.GetLineEnd(linesCount - 1)).Trim();
                while (CreateWorkingLayout(workingText + ELLIPSIS).LineCount > linesCount)
                {
                    var lastSpace = workingText.LastIndexOf(' ');
                    if (lastSpace == -1)
                    {
                        break;
                    }
                    workingText = workingText.Substring(0, lastSpace);
                }
                // We should do this in the loop above, but it's cheaper this way.
                workingText = _endPunctuationPattern.Matcher(workingText).ReplaceFirst("");
                workingText = workingText + ELLIPSIS;
                ellipsized = true;
            }
            if (!workingText.Equals(Text))
            {
                _programmaticChange = true;
                try
                {
                    SetText(workingText, BufferType.Normal);
                }
                finally
                {
                    _programmaticChange = false;
                }
            }
            _isStale = false;
            if (ellipsized != _isEllipsized)
            {
                _isEllipsized = ellipsized;
                foreach (var listener in _ellipsizeListeners)
                {
                    listener.EllipsizeStateChanged(ellipsized);
                }
            }
        }

        /**
       * Get how many lines of text we are allowed to display.
       */
        private int GetLinesCount()
        {
            if (EllipsizingLastFullyVisibleLine())
            {
                var fullyVisibleLinesCount = GetFullyVisibleLinesCount();
                return fullyVisibleLinesCount == -1 ? 1 : fullyVisibleLinesCount;
            }

            return MaxLines;
        }

        /**
        * Get how many lines of text we can display so their full height is visible.
        */
        private int GetFullyVisibleLinesCount()
        {
            var layout = CreateWorkingLayout("");
            var height = Height - PaddingTop - PaddingBottom;
            height -= _lineDecender;
            var lineHeight = layout.GetLineBottom(0);
            return height / lineHeight;
        }

        private Layout CreateWorkingLayout(String workingText)
        {
            return new StaticLayout(workingText, Paint,
                Width - PaddingLeft - PaddingRight,
                Layout.Alignment.AlignNormal, _lineSpacingMultiplier,
                _lineAdditionalVerticalPadding, false /* includepad */);
        }

        public override TextUtils.TruncateAt Ellipsize
        {
            set
            {

            }
        }
    }
}