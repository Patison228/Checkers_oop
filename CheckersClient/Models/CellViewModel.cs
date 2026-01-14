using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace CheckersClient.Models
{
    /// <summary>
    /// ViewModel для одной клетки доски шашек в WPF-клиенте.
    /// Содержит визуальное состояние клетки (подсветка, выбор, дамка) и уведомляет UI об изменениях.
    /// </summary>
    public class CellViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Хранит индекс строки и столбца клетки для быстрого доступа.
        /// </summary>
        private readonly int _row, _col;

        private bool _isKing;

        /// <summary>
        /// Текущее состояние шашки на клетке. 
        /// Возможные значения: <c>"None"</c>, <c>"White"</c>, <c>"Black"</c>.
        /// </summary>
        private string _pieceColor = "None";

        /// <summary>
        /// Локальное состояние UI: выделена ли клетка игроком.
        /// </summary>
        private bool _isSelected;

        /// <summary>
        /// Локальное состояние UI: доступен ли ход на эту клетку.
        /// </summary>
        private bool _isPossibleMove;

        /// <summary>
        /// Создаёт новую ViewModel для клетки с заданными координатами.
        /// </summary>
        /// <param name="row">Индекс строки (0-7).</param>
        /// <param name="col">Индекс столбца (0-7).</param>
        public CellViewModel(int row, int col)
        {
            _row = row;
            _col = col;
        }

        /// <summary>
        /// Неизменяемый индекс строки на доске (0-7).
        /// Строка 0 — верхний край доски (позиция чёрных).
        /// </summary>
        public int Row => _row;

        /// <summary>
        /// Неизменяемый индекс столбца на доске (0-7).
        /// Столбец 0 — левый край доски.
        /// </summary>
        public int Col => _col;

        /// <summary>
        /// Цвет шашки на клетке. Автоматически обновляет связанные визуальные свойства.
        /// Возможные значения: <c>"None"</c>, <c>"White"</c>, <c>"Black"</c>.
        /// </summary>
        public string PieceColor
        {
            get => _pieceColor;
            set
            {
                _pieceColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PieceWpfColor));
                OnPropertyChanged(nameof(PieceVisibility));
                OnPropertyChanged(nameof(Background));
            }
        }

        /// <summary>
        /// Признак дамки. <c>true</c> отображает красную точку в центре шашки.
        /// Синхронизируется с сервером через <see cref="CheckersModels.Models.Cell.IsKing"/>.
        /// </summary>
        public bool IsKing
        {
            get => _isKing;
            set
            {
                _isKing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KingIndicatorVisibility));
                OnPropertyChanged(nameof(Background));
            }
        }

        /// <summary>
        /// Временное состояние UI: выделена ли клетка текущим игроком (жёлтая подсветка).
        /// Управляется <see cref="CheckersClient.ViewModels.GameViewModel"/>.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Background));
            }
        }

        /// <summary>
        /// Временное состояние UI: доступен ли ход на эту клетку (зелёная подсветка).
        /// Показывает все легальные ходы для выбранной шашки.
        /// </summary>
        public bool IsPossibleMove
        {
            get => _isPossibleMove;
            set
            {
                _isPossibleMove = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Background));
            }
        }

        /// <summary>
        /// Фон клетки по приоритету:
        /// 1. Выделенная (жёлтый),
        /// 2. Возможный ход (зелёный),
        /// 3. Шахматная расцветка (тёмно-коричневый/кремовый).
        /// </summary>
        public Brush Background => IsSelected ? Brushes.LightYellow :
                                  IsPossibleMove ? Brushes.LightGreen :
                                  (Row + Col) % 2 == 0 ? Brushes.SaddleBrown : Brushes.Ivory;

        /// <summary>
        /// Цвет заливки шашки. Прозрачный для пустых клеток.
        /// </summary>
        public Brush PieceWpfColor => PieceColor switch
        {
            "White" => Brushes.WhiteSmoke,
            "Black" => Brushes.DimGray,
            _ => Brushes.Transparent
        };

        /// <summary>
        /// Видимость шашки. Скрыта (<c>Collapsed</c>) для пустых клеток.
        /// </summary>
        public Visibility PieceVisibility => PieceColor == "None" ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// Видимость красной точки дамки. Показывается только при <see cref="IsKing"/>.
        /// </summary>
        public Visibility KingIndicatorVisibility => IsKing ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Событие уведомления UI об изменении свойств клетки.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Вызывает событие <see cref="PropertyChanged"/> для указанного свойства.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
