﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Data;
using HandyControl.Expression.Drawing;
using HandyControl.Tools;

namespace HandyControl.Controls
{
    public class FlexPanel : Panel
    {
        private UVSize _uvConstraint;

        private int _lineCount;

        private readonly List<FlexItemInfo> _orderList = new List<FlexItemInfo>();

        #region Item

        public static readonly DependencyProperty OrderProperty = DependencyProperty.RegisterAttached(
            "Order", typeof(int), typeof(FlexPanel), new FrameworkPropertyMetadata(ValueBoxes.Int0Box, OnItemPropertyChanged));

        public static void SetOrder(DependencyObject element, int value)
            => element.SetValue(OrderProperty, value);

        public static int GetOrder(DependencyObject element)
            => (int)element.GetValue(OrderProperty);

        public static readonly DependencyProperty FlexGrowProperty = DependencyProperty.RegisterAttached(
            "FlexGrow", typeof(double), typeof(FlexPanel), new FrameworkPropertyMetadata(ValueBoxes.Double0Box, OnItemPropertyChanged), ValidateHelper.IsInRangeOfPosDoubleIncludeZero);

        public static void SetFlexGrow(DependencyObject element, double value)
            => element.SetValue(FlexGrowProperty, value);

        public static double GetFlexGrow(DependencyObject element)
            => (double)element.GetValue(FlexGrowProperty);

        public static readonly DependencyProperty FlexShrinkProperty = DependencyProperty.RegisterAttached(
            "FlexShrink", typeof(double), typeof(FlexPanel), new FrameworkPropertyMetadata(ValueBoxes.Double1Box, OnItemPropertyChanged));

        public static void SetFlexShrink(DependencyObject element, double value)
            => element.SetValue(FlexShrinkProperty, value);

        public static double GetFlexShrink(DependencyObject element)
            => (double)element.GetValue(FlexShrinkProperty);

        public static readonly DependencyProperty FlexBasisProperty = DependencyProperty.RegisterAttached(
            "FlexBasis", typeof(double), typeof(FlexPanel), new FrameworkPropertyMetadata(double.NaN, OnItemPropertyChanged));

        public static void SetFlexBasis(DependencyObject element, double value)
            => element.SetValue(FlexBasisProperty, value);

        public static double GetFlexBasis(DependencyObject element)
            => (double)element.GetValue(FlexBasisProperty);

        public static readonly DependencyProperty AlignSelfProperty = DependencyProperty.RegisterAttached(
            "AlignSelf", typeof(FlexItemAlignment), typeof(FlexPanel), new FrameworkPropertyMetadata(default(FlexItemAlignment), OnItemPropertyChanged));

        public static void SetAlignSelf(DependencyObject element, FlexItemAlignment value)
            => element.SetValue(AlignSelfProperty, value);

        #endregion

        #region Panel

        public static FlexItemAlignment GetAlignSelf(DependencyObject element)
            => (FlexItemAlignment)element.GetValue(AlignSelfProperty);

        public static readonly DependencyProperty FlexDirectionProperty = DependencyProperty.Register(
            "FlexDirection", typeof(FlexDirection), typeof(FlexPanel), new FrameworkPropertyMetadata(default(FlexDirection), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public FlexDirection FlexDirection
        {
            get => (FlexDirection)GetValue(FlexDirectionProperty);
            set => SetValue(FlexDirectionProperty, value);
        }

        public static readonly DependencyProperty FlexWrapProperty = DependencyProperty.Register(
            "FlexWrap", typeof(FlexWrap), typeof(FlexPanel), new FrameworkPropertyMetadata(default(FlexWrap), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public FlexWrap FlexWrap
        {
            get => (FlexWrap)GetValue(FlexWrapProperty);
            set => SetValue(FlexWrapProperty, value);
        }

        public static readonly DependencyProperty JustifyContentProperty = DependencyProperty.Register(
            "JustifyContent", typeof(FlexContentJustify), typeof(FlexPanel), new FrameworkPropertyMetadata(default(FlexContentJustify), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public FlexContentJustify JustifyContent
        {
            get => (FlexContentJustify)GetValue(JustifyContentProperty);
            set => SetValue(JustifyContentProperty, value);
        }

        public static readonly DependencyProperty AlignItemsProperty = DependencyProperty.Register(
            "AlignItems", typeof(FlexItemsAlignment), typeof(FlexPanel), new FrameworkPropertyMetadata(default(FlexItemsAlignment), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public FlexItemsAlignment AlignItems
        {
            get => (FlexItemsAlignment)GetValue(AlignItemsProperty);
            set => SetValue(AlignItemsProperty, value);
        }

        public static readonly DependencyProperty AlignContentProperty = DependencyProperty.Register(
            "AlignContent", typeof(FlexContentAlignment), typeof(FlexPanel), new FrameworkPropertyMetadata(default(FlexContentAlignment), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public FlexContentAlignment AlignContent
        {
            get => (FlexContentAlignment)GetValue(AlignContentProperty);
            set => SetValue(AlignContentProperty, value);
        }

        #endregion

        private static void OnItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if (VisualTreeHelper.GetParent(element) is FlexPanel p)
                {
                    p.InvalidateMeasure();
                }
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var curLineSize = new UVSize(FlexDirection);
            var panelSize = new UVSize(FlexDirection);
            _uvConstraint = new UVSize(FlexDirection, constraint);
            var childConstraint = new Size(constraint.Width, constraint.Height);
            _lineCount = 1;
            var children = InternalChildren;

            _orderList.Clear();
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null) continue;

                _orderList.Add(new FlexItemInfo(i, GetOrder(child)));
            }
            _orderList.Sort();

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[_orderList[i].Index];
                if (child == null) continue;

                child.Measure(childConstraint);

                var sz = new UVSize(FlexDirection, child.DesiredSize);

                if (FlexWrap == FlexWrap.NoWrap) //continue to accumulate a line
                {
                    curLineSize.U += sz.U;
                    curLineSize.V = Math.Max(sz.V, curLineSize.V);
                }
                else
                {
                    if (MathHelper.GreaterThan(curLineSize.U + sz.U, _uvConstraint.U)) //need to switch to another line
                    {
                        panelSize.U = Math.Max(curLineSize.U, panelSize.U);
                        panelSize.V += curLineSize.V;
                        curLineSize = sz;
                        _lineCount++;

                        if (MathHelper.GreaterThan(sz.U, _uvConstraint.U)) //the element is wider then the constrint - give it a separate line                    
                        {
                            panelSize.U = Math.Max(sz.U, panelSize.U);
                            panelSize.V += sz.V;
                            curLineSize = new UVSize(FlexDirection);
                            _lineCount++;
                        }
                    }
                    else //continue to accumulate a line
                    {
                        curLineSize.U += sz.U;
                        curLineSize.V = Math.Max(sz.V, curLineSize.V);
                    }
                }
            }

            //the last line size, if any should be added
            panelSize.U = Math.Max(curLineSize.U, panelSize.U);
            panelSize.V += curLineSize.V;

            //go from UV space to W/H space
            return new Size(panelSize.Width, panelSize.Height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var uvFinalSize = new UVSize(FlexDirection, arrangeSize);
            if (MathHelper.IsZero(uvFinalSize.U) || MathHelper.IsZero(uvFinalSize.V)) return arrangeSize;

            // init status
            var children = InternalChildren;
            var lineIndex = 0;

            var curLineSizeArr = new UVSize[_lineCount];
            curLineSizeArr[0] = new UVSize(FlexDirection);

            var lastInLineArr = new int[_lineCount];
            for (var i = 0; i < _lineCount; i++)
            {
                lastInLineArr[i] = int.MaxValue;
            }

            // calculate line max U space
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[_orderList[i].Index];
                if (child == null) continue;

                var sz = new UVSize(FlexDirection, child.DesiredSize);

                if (FlexWrap == FlexWrap.NoWrap)
                {
                    curLineSizeArr[lineIndex].U += sz.U;
                    curLineSizeArr[lineIndex].V = Math.Max(sz.V, curLineSizeArr[lineIndex].V);
                }
                else
                {
                    if (MathHelper.GreaterThan(curLineSizeArr[lineIndex].U + sz.U, uvFinalSize.U)) //need to switch to another line
                    {
                        lastInLineArr[lineIndex] = i;
                        lineIndex++;
                        curLineSizeArr[lineIndex] = sz;

                        if (MathHelper.GreaterThan(sz.U, uvFinalSize.U)) //the element is wider then the constraint - give it a separate line                    
                        {
                            //switch to next line which only contain one element
                            lastInLineArr[lineIndex] = i;
                            lineIndex++;
                            curLineSizeArr[lineIndex] = new UVSize(FlexDirection);
                        }
                    }
                    else //continue to accumulate a line
                    {
                        curLineSizeArr[lineIndex].U += sz.U;
                        curLineSizeArr[lineIndex].V = Math.Max(sz.V, curLineSizeArr[lineIndex].V);
                    }
                }
            }

            // init status
            var scaleU = Math.Min(_uvConstraint.U / uvFinalSize.U, 1);
            var firstInLine = 0;
            var wrapReverseAdd = 0;
            var wrapReverseFlag = FlexWrap == FlexWrap.WrapReverse ? -1 : 1;
            var accumulatedFlag = FlexWrap == FlexWrap.WrapReverse ? 1 : 0;
            var itemsU = .0;
            var accumulatedV = .0;
            var freeV = uvFinalSize.V;
            foreach (var flexSize in curLineSizeArr)
            {
                freeV -= flexSize.V;
            }
            var freeItemV = freeV;

            // calculate status
            var lineFreeVArr = new double[_lineCount];
            switch (AlignContent)
            {
                case FlexContentAlignment.Stretch:
                    if (_lineCount > 1)
                    {
                        freeItemV = freeV / _lineCount;
                        for (var i = 0; i < _lineCount; i++)
                        {
                            lineFreeVArr[i] = freeItemV;
                        }

                        accumulatedV = FlexWrap == FlexWrap.WrapReverse ? uvFinalSize.V - curLineSizeArr[0].V - lineFreeVArr[0] : 0;
                    }
                    break;
                case FlexContentAlignment.FlexStart:
                    if (FlexWrap == FlexWrap.WrapReverse)
                    {
                        wrapReverseAdd = 1;
                    }

                    if (_lineCount > 1)
                    {
                        accumulatedV = FlexWrap == FlexWrap.WrapReverse ? uvFinalSize.V - curLineSizeArr[0].V : 0;
                    }
                    else
                    {
                        wrapReverseAdd = 0;
                    }
                    break;
                case FlexContentAlignment.FlexEnd:
                    if (FlexWrap != FlexWrap.WrapReverse)
                    {
                        wrapReverseAdd = 1;
                    }

                    if (_lineCount > 1)
                    {
                        accumulatedV = FlexWrap == FlexWrap.WrapReverse ? uvFinalSize.V - curLineSizeArr[0].V - freeV : freeV;
                    }
                    else
                    {
                        wrapReverseAdd = 0;
                    }
                    break;
                case FlexContentAlignment.Center:
                    if (_lineCount > 1)
                    {
                        accumulatedV = FlexWrap == FlexWrap.WrapReverse ? uvFinalSize.V - curLineSizeArr[0].V - freeV * 0.5 : freeV * 0.5;
                    }
                    break;
                case FlexContentAlignment.SpaceBetween:
                    if (_lineCount > 1)
                    {
                        freeItemV = freeV / (_lineCount - 1);
                        for (var i = 0; i < _lineCount - 1; i++)
                        {
                            lineFreeVArr[i] = freeItemV;
                        }

                        accumulatedV = FlexWrap == FlexWrap.WrapReverse ? uvFinalSize.V - curLineSizeArr[0].V : 0;
                    }
                    break;
                case FlexContentAlignment.SpaceAround:
                    if (_lineCount > 1)
                    {
                        freeItemV = freeV / _lineCount * 0.5;
                        for (var i = 0; i < _lineCount - 1; i++)
                        {
                            lineFreeVArr[i] = freeItemV * 2;
                        }

                        accumulatedV = FlexWrap == FlexWrap.WrapReverse ? uvFinalSize.V - curLineSizeArr[0].V - freeItemV : freeItemV;
                    }
                    break;
            }

            // clear status
            lineIndex = 0;

            // arrange line
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[_orderList[i].Index];
                if (child == null) continue;

                var sz = new UVSize(FlexDirection, child.DesiredSize);

                if (FlexWrap != FlexWrap.NoWrap)
                {
                    if (i >= lastInLineArr[lineIndex]) //need to switch to another line
                    {
                        ArrangeLine(new FlexLineInfo
                        {
                            ItemsU = itemsU,
                            OffsetV = accumulatedV + freeItemV * wrapReverseAdd,
                            LineV = curLineSizeArr[lineIndex].V,
                            LineFreeV = freeItemV,
                            LineU = uvFinalSize.U,
                            ItemStartIndex = firstInLine,
                            ItemEndIndex = i,
                            ScaleU = scaleU
                        });

                        accumulatedV += (lineFreeVArr[lineIndex] + curLineSizeArr[lineIndex + accumulatedFlag].V) * wrapReverseFlag;
                        lineIndex++;
                        itemsU = 0;

                        if (i >= lastInLineArr[lineIndex]) //the element is wider then the constraint - give it a separate line                    
                        {
                            //switch to next line which only contain one element
                            ArrangeLine(new FlexLineInfo
                            {
                                ItemsU = itemsU,
                                OffsetV = accumulatedV + freeItemV * wrapReverseAdd,
                                LineV = curLineSizeArr[lineIndex].V,
                                LineFreeV = freeItemV,
                                LineU = uvFinalSize.U,
                                ItemStartIndex = i,
                                ItemEndIndex = ++i,
                                ScaleU = scaleU
                            });

                            accumulatedV += (lineFreeVArr[lineIndex] + curLineSizeArr[lineIndex + accumulatedFlag].V) * wrapReverseFlag;
                            lineIndex++;
                            itemsU = 0;
                        }
                        firstInLine = i;
                    }
                }

                itemsU += sz.U;
            }

            // arrange the last line, if any
            if (firstInLine < children.Count)
            {
                ArrangeLine(new FlexLineInfo
                {
                    ItemsU = itemsU,
                    OffsetV = accumulatedV + freeItemV * wrapReverseAdd,
                    LineV = curLineSizeArr[lineIndex].V,
                    LineFreeV = freeItemV,
                    LineU = uvFinalSize.U,
                    ItemStartIndex = firstInLine,
                    ItemEndIndex = children.Count,
                    ScaleU = scaleU
                });
            }

            return arrangeSize;
        }

        private void ArrangeLine(FlexLineInfo lineInfo)
        {
            var isHorizontal = FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse;
            var isReverse = FlexDirection == FlexDirection.RowReverse || FlexDirection == FlexDirection.ColumnReverse;

            // calculate initial u
            var u = .0;
            if (isReverse)
            {
                u = JustifyContent switch
                {
                    FlexContentJustify.FlexStart => lineInfo.LineU,
                    FlexContentJustify.SpaceBetween => lineInfo.LineU,
                    FlexContentJustify.SpaceAround => lineInfo.LineU,
                    FlexContentJustify.FlexEnd => lineInfo.ItemsU,
                    FlexContentJustify.Center => (lineInfo.LineU + lineInfo.ItemsU) * 0.5,
                    _ => u
                };
            }
            else
            {
                u = JustifyContent switch
                {
                    FlexContentJustify.FlexEnd => lineInfo.LineU - lineInfo.ItemsU,
                    FlexContentJustify.Center => (lineInfo.LineU - lineInfo.ItemsU) * 0.5,
                    _ => u
                };
            }
            u *= lineInfo.ScaleU;

            // calculate offset u
            var itemCount = lineInfo.ItemEndIndex - lineInfo.ItemStartIndex;
            var offsetUArr = new double[itemCount];
            if (JustifyContent == FlexContentJustify.SpaceBetween)
            {
                var freeItemU = (lineInfo.LineU - lineInfo.ItemsU) / (itemCount - 1);
                for (var i = 1; i < itemCount; i++)
                {
                    offsetUArr[i] = freeItemU;
                }
            }
            else if (JustifyContent == FlexContentJustify.SpaceAround)
            {
                var freeItemU = (lineInfo.LineU - lineInfo.ItemsU) / itemCount * 0.5;
                offsetUArr[0] = freeItemU;
                for (var i = 1; i < itemCount; i++)
                {
                    offsetUArr[i] = freeItemU * 2;
                }
            }

            var children = InternalChildren;
            for (int i = lineInfo.ItemStartIndex, j = 0; i < lineInfo.ItemEndIndex; i++, j++)
            {
                var child = children[_orderList[i].Index];
                if (child == null) continue;

                var childSize = new UVSize(FlexDirection, isHorizontal ?
                    new Size(child.DesiredSize.Width * lineInfo.ScaleU, child.DesiredSize.Height) :
                    new Size(child.DesiredSize.Width, child.DesiredSize.Height * lineInfo.ScaleU));

                if (isReverse)
                {
                    u -= childSize.U;
                    u -= offsetUArr[j];
                }
                else
                {
                    u += offsetUArr[j];
                }

                var v = lineInfo.OffsetV;
                var alignSelf = GetAlignSelf(child);
                FlexItemsAlignment alignment;
                if (alignSelf == FlexItemAlignment.Auto)
                {
                    alignment = AlignItems;
                }
                else
                {
                    alignment = (FlexItemsAlignment)alignSelf;
                }

                switch (alignment)
                {
                    case FlexItemsAlignment.Stretch:
                        if (_lineCount == 1 && FlexWrap == FlexWrap.NoWrap)
                        {
                            childSize.V = lineInfo.LineV + lineInfo.LineFreeV;
                        }
                        else
                        {
                            childSize.V = lineInfo.LineV;
                        }
                        break;
                    case FlexItemsAlignment.FlexEnd:
                        v += lineInfo.LineV - childSize.V;
                        break;
                    case FlexItemsAlignment.Center:
                        v += (lineInfo.LineV - childSize.V) * 0.5;
                        break;
                }

                child.Arrange(isHorizontal ? new Rect(u, v, childSize.U, childSize.V) : new Rect(v, u, childSize.V, childSize.U));

                if (!isReverse)
                {
                    u += childSize.U;
                }
            }
        }

        private readonly struct FlexItemInfo : IComparable<FlexItemInfo>
        {
            public FlexItemInfo(int index, int order)
            {
                Index = index;
                Order = order;
            }

            private int Order { get; }

            public int Index { get; }

            public int CompareTo(FlexItemInfo other) => Order.CompareTo(other.Order);
        }

        private struct FlexLineInfo
        {
            public double ItemsU { get; set; }

            public double OffsetV { get; set; }

            public double LineU { get; set; }

            public double LineV { get; set; }

            public double LineFreeV { get; set; }

            public int ItemStartIndex { get; set; }

            public int ItemEndIndex { get; set; }

            public double ScaleU { get; set; }
        }

        private struct UVSize
        {
            public UVSize(FlexDirection direction, Size size)
            {
                U = V = 0d;
                FlexDirection = direction;
                Width = size.Width;
                Height = size.Height;
            }

            public UVSize(FlexDirection direction)
            {
                U = V = 0d;
                FlexDirection = direction;
            }

            public double U { get; set; }

            public double V { get; set; }

            private FlexDirection FlexDirection { get; }

            public double Width
            {
                get => FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse ? U : V;
                private set
                {
                    if (FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse)
                    {
                        U = value;
                    }
                    else
                    {
                        V = value;
                    }
                }
            }

            public double Height
            {
                get => FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse ? V : U;
                private set
                {
                    if (FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse)
                    {
                        V = value;
                    }
                    else
                    {
                        U = value;
                    }
                }
            }
        }
    }
}
