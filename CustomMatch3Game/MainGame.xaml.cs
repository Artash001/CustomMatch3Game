﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace CustomMatch3Game
{
    /// <summary>
    /// Interaction logic for MainGame.xaml
    /// </summary>
    public partial class MainGame : UserControl
    {
        private readonly GameLogic _game;

        public MainGame()
        {
            InitializeComponent();
            _game = new GameLogic(RegisterTile, UnregisterTile, DropAnimation);
            DataContext = _game;
        }

        private void RegisterTile(Tile tile)
        {
            tile.Shape.Height = GameCanvas.Height / 8;
            tile.Shape.Width = GameCanvas.Width / 8;
            tile.Shape.RenderTransform =
                new ScaleTransform(1.0, 1.0, tile.Shape.Height / 2, tile.Shape.Width / 2);
            GameCanvas.Children.Add(tile.Shape);
            Canvas.SetTop(tile.Shape, tile.Top * tile.Shape.Height);
            Canvas.SetLeft(tile.Shape, tile.Left * tile.Shape.Width);
        }


        private int _dropAnimationRegister;

        private void DropAnimation(Tile tile)
        {
            _dropAnimationRegister++;
            var animTop = new DoubleAnimation
            {
                To = tile.Top * tile.Shape.Height,
                Duration = TimeSpan.FromMilliseconds(200),
            };
            animTop.Completed += delegate
            {
                _dropAnimationRegister--;
                if (_dropAnimationRegister != 0) return;
                _game.FillBoard(RegisterTile);
                _game.RemoveMatches(DeleteAnimation);
            };
            tile.Shape.BeginAnimation(Canvas.TopProperty, animTop);
        }

        private void StartSelectionAnimation(Tile tile)
        {
            var anim = new DoubleAnimation
            {
                From = 1.0,
                To = 0.8,
                Duration = TimeSpan.FromMilliseconds(300),
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true,
            };
            tile.Shape.RenderTransform.BeginAnimation(
                ScaleTransform.ScaleXProperty, anim);
            tile.Shape.RenderTransform.BeginAnimation(
                ScaleTransform.ScaleYProperty, anim);
        }

        private void StopSelectionAnimation(Tile tile)
        {
            tile.Shape.RenderTransform.BeginAnimation(
                ScaleTransform.ScaleXProperty, null);
            tile.Shape.RenderTransform.BeginAnimation(
                ScaleTransform.ScaleYProperty, null);
        }

        private int _deleteAnimationRegister;

        private void DeleteAnimation(Tile tile)
        {
            _deleteAnimationRegister += 2;
            var anim = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300),
            };
            anim.Completed += delegate
            {
                _deleteAnimationRegister--;
                if (_deleteAnimationRegister == 0)
                {
                    _game.DeleteAndDropTiles(
                        DropAnimation, RegisterTile, UnregisterTile);
                }
            };
            tile.Shape.RenderTransform.BeginAnimation(
                ScaleTransform.ScaleXProperty, anim);
            tile.Shape.RenderTransform.BeginAnimation(
                ScaleTransform.ScaleYProperty, anim);
        }

        private int _successAnimationRegister;

        private void OnSuccessAnimationComplete(object o, EventArgs e)
        {
            _successAnimationRegister--;
            if (_successAnimationRegister == 0)
            {
                _game.RemoveMatches(DeleteAnimation);
            }
        }

        private void SuccessAnimation(Tile first, Tile second)
        {
            _successAnimationRegister += 2;
            AnimateSwap(first, second, OnSuccessAnimationComplete);
        }

        private void AnimateSwap(
            Tile first, Tile second, Action<object, EventArgs> onCompleted)
        {
            var dt = Math.Sign(Math.Abs(first.Top - second.Top));
            var dl = Math.Sign(Math.Abs(first.Left - second.Left));
            var animFirst = new DoubleAnimation
            {
                Duration = TimeSpan.FromMilliseconds(200),
            };
            var animSecond = new DoubleAnimation
            {
                Duration = TimeSpan.FromMilliseconds(200),
            };
            animFirst.Completed += (o, ea) => onCompleted(o, ea);
            animSecond.Completed += (o, ea) => onCompleted(o, ea);
            if (dt == 1)
            {
                animSecond.To = second.Top * second.Shape.Height;
                animFirst.To = first.Top * first.Shape.Height;
                first.Shape.BeginAnimation(Canvas.TopProperty, animFirst);
                second.Shape.BeginAnimation(Canvas.TopProperty, animSecond);
            }
            else if (dl == 1)
            {
                animSecond.To = second.Left * second.Shape.Width;
                animFirst.To = first.Left * first.Shape.Width;
                first.Shape.BeginAnimation(Canvas.LeftProperty, animFirst);
                second.Shape.BeginAnimation(Canvas.LeftProperty, animSecond);
            }
            else
            {
                throw new InvalidOperationException("Moving diagonally");
            }
        }

        private int _failAnimationRegister;

        private void FailAnimation(Tile first, Tile second)
        {
            _failAnimationRegister += 2;
            first.SwapCoordinates(ref second);
            AnimateSwap(
                first, second, (o1, e1) =>
                {
                    _failAnimationRegister--;
                    if (_failAnimationRegister != 0) return;
                    first.SwapCoordinates(ref second);
                    _failAnimationRegister += 2;
                    AnimateSwap(
                        first, second, (o2, e2) => { _failAnimationRegister--; });
                });
        }

        private void UnregisterTile(Tile tile)
        {
            GameCanvas.Children.Remove(tile.Shape);
        }

        private Tile _selected;

        private void GameCanvas_OnMouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (_failAnimationRegister + _deleteAnimationRegister +
                _successAnimationRegister + _dropAnimationRegister > 0)
            {
                return;
            }

            if (!(e.OriginalSource is TileShape ts)) return;
            var t = (Tile)ts.Tag;
            if (t.Selected)
            {
                t.Selected = false;
                StopSelectionAnimation(t);
                _selected = null;
            }
            else
            {
                if (_selected != null)
                {
                    var tempTile = _selected;
                    _selected.Selected = false;
                    StopSelectionAnimation(_selected);
                    _selected = null;
                    _game.TrySwapTiles(t, tempTile, SuccessAnimation, FailAnimation);
                }
                else
                {
                    t.Selected = true;
                    _selected = t;
                    StartSelectionAnimation(t);
                }
            }
        }
    }
}
