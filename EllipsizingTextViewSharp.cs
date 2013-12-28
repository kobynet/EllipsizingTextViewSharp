using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Widget;
using Java.Lang;
using Java.Util.Regex;
using String = System.String;

namespace Kobynet
{
    public class EllipsizingTextView : TextView
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


        public EllipsizingTextView(Context context)
            : this(context, null)
        {
        }

        public EllipsizingTextView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        {
        }

        public EllipsizingTextView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            base.Ellipsize = null;
            context.ObtainStyledAttributes(attrs, new int[] { });

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