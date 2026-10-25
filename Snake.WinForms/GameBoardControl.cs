using System.Drawing.Drawing2D;
using Snake.UseCases;

namespace Snake.WinForms;

public class GameBoardControl : Control
{
    private GameBoardViewModel? _viewModel;

    public GameBoardControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(251, 246, 232);
        AccessibleRole = AccessibleRole.Graphic;
        AccessibleName = "貪吃蛇遊戲棋盤";
    }

    public void Render(GameBoardViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        AccessibleDescription = viewModel.IsVisible
            ? $"{viewModel.Rows} 乘 {viewModel.Columns} 棋盤"
            : "棋盤目前未顯示";
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (_viewModel is null || !_viewModel.IsVisible)
        {
            return;
        }

        Graphics graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        float availableWidth = Math.Max(1, ClientSize.Width - 24);
        float availableHeight = Math.Max(1, ClientSize.Height - 24);
        float cellSize = Math.Min(
            availableWidth / _viewModel.Columns,
            availableHeight / _viewModel.Rows);
        float boardWidth = cellSize * _viewModel.Columns;
        float boardHeight = cellSize * _viewModel.Rows;
        float originX = (ClientSize.Width - boardWidth) / 2f;
        float originY = (ClientSize.Height - boardHeight) / 2f;
        RectangleF frame = new RectangleF(
            originX - 7,
            originY - 7,
            boardWidth + 14,
            boardHeight + 14);

        using SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(32, 34, 91, 68));
        graphics.FillRectangle(
            shadowBrush,
            frame.X + 5,
            frame.Y + 9,
            frame.Width,
            frame.Height);
        using SolidBrush frameBrush = new SolidBrush(Color.FromArgb(63, 126, 104));
        graphics.FillRectangle(frameBrush, frame);

        DrawCells(graphics, originX, originY, cellSize);
        DrawWalls(graphics, originX, originY, cellSize);
        DrawDoors(graphics, originX, originY, cellSize);
        DrawObjects(graphics, originX, originY, cellSize);
        DrawSnake(graphics, originX, originY, cellSize);
    }

    private void DrawCells(Graphics graphics, float originX, float originY, float cellSize)
    {
        if (_viewModel is null)
        {
            return;
        }

        using SolidBrush lightBrush = new SolidBrush(Color.FromArgb(255, 249, 223));
        using SolidBrush alternateBrush = new SolidBrush(Color.FromArgb(223, 245, 216));
        for (int row = 0; row < _viewModel.Rows; row++)
        {
            for (int column = 0; column < _viewModel.Columns; column++)
            {
                RectangleF cell = GetCellRectangle(column, row, originX, originY, cellSize, 1.1f);
                graphics.FillRectangle((row + column) % 2 == 0 ? lightBrush : alternateBrush, cell);
            }
        }
    }

    private void DrawWalls(Graphics graphics, float originX, float originY, float cellSize)
    {
        if (_viewModel is null)
        {
            return;
        }

        using SolidBrush wallBrush = new SolidBrush(Color.FromArgb(113, 139, 158));
        using Pen highlightPen = new Pen(Color.FromArgb(150, 225, 235, 240), Math.Max(1.5f, cellSize * 0.045f));
        foreach (GridCellViewModel wall in _viewModel.WallCells)
        {
            RectangleF cell = GetCellRectangle(wall.X, wall.Y, originX, originY, cellSize, cellSize * 0.08f);
            graphics.FillRectangle(wallBrush, cell);
            graphics.DrawLine(
                highlightPen,
                cell.Left + cell.Width * 0.12f,
                cell.Top + cell.Height * 0.24f,
                cell.Right - cell.Width * 0.12f,
                cell.Top + cell.Height * 0.24f);
        }
    }

    private void DrawDoors(Graphics graphics, float originX, float originY, float cellSize)
    {
        if (_viewModel is null)
        {
            return;
        }

        foreach (DoorViewModel door in _viewModel.Doors)
        {
            RectangleF cell = GetCellRectangle(
                door.Position.X,
                door.Position.Y,
                originX,
                originY,
                cellSize,
                cellSize * 0.1f);
            if (door.State == "Open" || door.State == "CloseDeferred")
            {
                using Pen openPen = new Pen(Color.FromArgb(200, 137, 5), Math.Max(2f, cellSize * 0.07f));
                openPen.DashStyle = DashStyle.Dash;
                graphics.DrawRectangle(openPen, cell.X, cell.Y, cell.Width, cell.Height);
                continue;
            }

            Color doorColor = door.State == "ClosingWarning"
                ? Color.FromArgb(255, 231, 98)
                : Color.FromArgb(255, 195, 45);
            using SolidBrush doorBrush = new SolidBrush(doorColor);
            using Pen doorPen = new Pen(Color.FromArgb(200, 137, 5), Math.Max(2f, cellSize * 0.06f));
            if (door.State == "ClosingWarning")
            {
                doorPen.DashStyle = DashStyle.Dot;
            }

            graphics.FillRectangle(doorBrush, cell);
            graphics.DrawRectangle(doorPen, cell.X, cell.Y, cell.Width, cell.Height);
            for (int stripe = -1; stripe < 4; stripe++)
            {
                float x = cell.Left + stripe * cell.Width * 0.38f;
                graphics.DrawLine(
                    doorPen,
                    x,
                    cell.Bottom,
                    x + cell.Width * 0.65f,
                    cell.Top);
            }
        }
    }

    private void DrawObjects(Graphics graphics, float originX, float originY, float cellSize)
    {
        if (_viewModel is null)
        {
            return;
        }

        foreach (BoardObjectViewModel gameObject in _viewModel.Objects)
        {
            RectangleF cell = GetCellRectangle(
                gameObject.Position.X,
                gameObject.Position.Y,
                originX,
                originY,
                cellSize,
                cellSize * 0.08f);
            switch (gameObject.Type)
            {
                case GameObjectType.Strawberry:
                    DrawStrawberry(graphics, cell);
                    break;
                case GameObjectType.SpecialFood:
                    DrawBlueberries(graphics, cell);
                    break;
                case GameObjectType.Shield:
                    DrawShield(graphics, cell);
                    break;
                case GameObjectType.Fire:
                    DrawFire(graphics, cell);
                    break;
                case GameObjectType.Bomb:
                    DrawBomb(graphics, cell);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(gameObject.Type),
                        gameObject.Type,
                        "不支援的遊戲物件圖案。");
            }
        }
    }

    private void DrawStrawberry(Graphics graphics, RectangleF cell)
    {
        RectangleF berry = new RectangleF(
            cell.Left + cell.Width * 0.23f,
            cell.Top + cell.Height * 0.25f,
            cell.Width * 0.54f,
            cell.Height * 0.58f);
        using SolidBrush berryBrush = new SolidBrush(Color.FromArgb(231, 69, 78));
        using Pen outlinePen = new Pen(Color.FromArgb(164, 43, 52), Math.Max(1.5f, cell.Width * 0.04f));
        graphics.FillEllipse(berryBrush, berry);
        graphics.DrawEllipse(outlinePen, berry);

        PointF[] leaves =
        {
            new PointF(cell.Left + cell.Width * 0.28f, cell.Top + cell.Height * 0.34f),
            new PointF(cell.Left + cell.Width * 0.5f, cell.Top + cell.Height * 0.12f),
            new PointF(cell.Left + cell.Width * 0.72f, cell.Top + cell.Height * 0.34f),
            new PointF(cell.Left + cell.Width * 0.5f, cell.Top + cell.Height * 0.29f)
        };
        using SolidBrush leafBrush = new SolidBrush(Color.FromArgb(48, 151, 72));
        graphics.FillPolygon(leafBrush, leaves);
        using SolidBrush seedBrush = new SolidBrush(Color.FromArgb(255, 231, 126));
        float seedSize = Math.Max(2f, cell.Width * 0.045f);
        graphics.FillEllipse(seedBrush, berry.Left + berry.Width * 0.28f, berry.Top + berry.Height * 0.38f, seedSize, seedSize);
        graphics.FillEllipse(seedBrush, berry.Left + berry.Width * 0.62f, berry.Top + berry.Height * 0.52f, seedSize, seedSize);
    }

    private void DrawBlueberries(Graphics graphics, RectangleF cell)
    {
        using SolidBrush berryBrush = new SolidBrush(Color.FromArgb(70, 91, 184));
        using Pen outlinePen = new Pen(Color.FromArgb(37, 55, 127), Math.Max(1.5f, cell.Width * 0.035f));
        float berrySize = cell.Width * 0.42f;
        RectangleF leftBerry = new RectangleF(
            cell.Left + cell.Width * 0.16f,
            cell.Top + cell.Height * 0.38f,
            berrySize,
            berrySize);
        RectangleF rightBerry = new RectangleF(
            cell.Left + cell.Width * 0.43f,
            cell.Top + cell.Height * 0.28f,
            berrySize,
            berrySize);
        graphics.FillEllipse(berryBrush, leftBerry);
        graphics.DrawEllipse(outlinePen, leftBerry);
        graphics.FillEllipse(berryBrush, rightBerry);
        graphics.DrawEllipse(outlinePen, rightBerry);
        using SolidBrush crownBrush = new SolidBrush(Color.FromArgb(190, 211, 240));
        graphics.FillEllipse(
            crownBrush,
            rightBerry.Left + rightBerry.Width * 0.36f,
            rightBerry.Top + rightBerry.Height * 0.12f,
            rightBerry.Width * 0.25f,
            rightBerry.Height * 0.15f);
    }

    private void DrawShield(Graphics graphics, RectangleF cell)
    {
        PointF[] shield =
        {
            new PointF(cell.Left + cell.Width * 0.5f, cell.Top + cell.Height * 0.1f),
            new PointF(cell.Left + cell.Width * 0.78f, cell.Top + cell.Height * 0.22f),
            new PointF(cell.Left + cell.Width * 0.73f, cell.Top + cell.Height * 0.64f),
            new PointF(cell.Left + cell.Width * 0.5f, cell.Top + cell.Height * 0.88f),
            new PointF(cell.Left + cell.Width * 0.27f, cell.Top + cell.Height * 0.64f),
            new PointF(cell.Left + cell.Width * 0.22f, cell.Top + cell.Height * 0.22f)
        };
        using SolidBrush shieldBrush = new SolidBrush(Color.FromArgb(93, 178, 214));
        using Pen outlinePen = new Pen(Color.FromArgb(21, 82, 117), Math.Max(2f, cell.Width * 0.05f));
        graphics.FillPolygon(shieldBrush, shield);
        graphics.DrawPolygon(outlinePen, shield);
        graphics.DrawLine(
            outlinePen,
            cell.Left + cell.Width * 0.5f,
            cell.Top + cell.Height * 0.18f,
            cell.Left + cell.Width * 0.5f,
            cell.Top + cell.Height * 0.76f);
    }

    private void DrawFire(Graphics graphics, RectangleF cell)
    {
        using GraphicsPath outerPath = new GraphicsPath();
        outerPath.AddBezier(
            cell.Left + cell.Width * 0.5f,
            cell.Top + cell.Height * 0.08f,
            cell.Left + cell.Width * 0.8f,
            cell.Top + cell.Height * 0.42f,
            cell.Left + cell.Width * 0.84f,
            cell.Top + cell.Height * 0.76f,
            cell.Left + cell.Width * 0.5f,
            cell.Top + cell.Height * 0.9f);
        outerPath.AddBezier(
            cell.Left + cell.Width * 0.5f,
            cell.Top + cell.Height * 0.9f,
            cell.Left + cell.Width * 0.16f,
            cell.Top + cell.Height * 0.76f,
            cell.Left + cell.Width * 0.24f,
            cell.Top + cell.Height * 0.38f,
            cell.Left + cell.Width * 0.5f,
            cell.Top + cell.Height * 0.08f);
        outerPath.CloseFigure();
        using SolidBrush outerBrush = new SolidBrush(Color.FromArgb(240, 90, 48));
        graphics.FillPath(outerBrush, outerPath);
        using SolidBrush innerBrush = new SolidBrush(Color.FromArgb(255, 204, 52));
        graphics.FillEllipse(
            innerBrush,
            cell.Left + cell.Width * 0.37f,
            cell.Top + cell.Height * 0.5f,
            cell.Width * 0.27f,
            cell.Height * 0.32f);
    }

    private void DrawBomb(Graphics graphics, RectangleF cell)
    {
        RectangleF bomb = new RectangleF(
            cell.Left + cell.Width * 0.2f,
            cell.Top + cell.Height * 0.3f,
            cell.Width * 0.58f,
            cell.Height * 0.58f);
        using SolidBrush bombBrush = new SolidBrush(Color.FromArgb(31, 42, 50));
        using Pen outlinePen = new Pen(Color.FromArgb(8, 18, 25), Math.Max(1.5f, cell.Width * 0.04f));
        graphics.FillEllipse(bombBrush, bomb);
        graphics.DrawEllipse(outlinePen, bomb);
        using Pen fusePen = new Pen(Color.FromArgb(125, 82, 43), Math.Max(2f, cell.Width * 0.055f));
        graphics.DrawArc(
            fusePen,
            cell.Left + cell.Width * 0.52f,
            cell.Top + cell.Height * 0.12f,
            cell.Width * 0.3f,
            cell.Height * 0.34f,
            170,
            120);
        using SolidBrush sparkBrush = new SolidBrush(Color.FromArgb(255, 185, 39));
        graphics.FillEllipse(
            sparkBrush,
            cell.Left + cell.Width * 0.76f,
            cell.Top + cell.Height * 0.08f,
            cell.Width * 0.12f,
            cell.Height * 0.12f);
    }

    private void DrawSnake(Graphics graphics, float originX, float originY, float cellSize)
    {
        if (_viewModel is null)
        {
            return;
        }

        List<GridCellViewModel> snakeCells = _viewModel.SnakeCells;
        for (int index = snakeCells.Count - 1; index >= 0; index--)
        {
            GridCellViewModel segment = snakeCells[index];
            float inset = index == 0 ? cellSize * 0.09f : cellSize * 0.14f;
            RectangleF cell = GetCellRectangle(
                segment.X,
                segment.Y,
                originX,
                originY,
                cellSize,
                inset);
            using GraphicsPath path = CreateRoundedRectangle(cell, Math.Max(4f, cellSize * 0.2f));
            using LinearGradientBrush snakeBrush = new LinearGradientBrush(
                cell,
                Color.FromArgb(163, 236, 80),
                Color.FromArgb(32, 168, 72),
                45f);
            using Pen borderPen = new Pen(Color.FromArgb(20, 131, 62), Math.Max(1.5f, cellSize * 0.045f));
            graphics.FillPath(snakeBrush, path);
            graphics.DrawPath(borderPen, path);

            if (index == 0)
            {
                DrawSnakeFace(graphics, cell, cellSize);
            }
        }
    }

    private void DrawSnakeFace(Graphics graphics, RectangleF head, float cellSize)
    {
        float eyeSize = Math.Max(3f, cellSize * 0.1f);
        float eyeY = head.Top + head.Height * 0.25f;
        float firstEyeX = head.Left + head.Width * 0.3f;
        float secondEyeX = head.Left + head.Width * 0.62f;
        using SolidBrush eyeWhiteBrush = new SolidBrush(Color.White);
        using SolidBrush pupilBrush = new SolidBrush(Color.FromArgb(23, 61, 104));
        graphics.FillEllipse(eyeWhiteBrush, firstEyeX, eyeY, eyeSize * 1.35f, eyeSize * 1.35f);
        graphics.FillEllipse(eyeWhiteBrush, secondEyeX, eyeY, eyeSize * 1.35f, eyeSize * 1.35f);
        graphics.FillEllipse(pupilBrush, firstEyeX + eyeSize * 0.42f, eyeY + eyeSize * 0.4f, eyeSize * 0.55f, eyeSize * 0.55f);
        graphics.FillEllipse(pupilBrush, secondEyeX + eyeSize * 0.42f, eyeY + eyeSize * 0.4f, eyeSize * 0.55f, eyeSize * 0.55f);
    }

    private RectangleF GetCellRectangle(
        int column,
        int row,
        float originX,
        float originY,
        float cellSize,
        float inset)
    {
        return new RectangleF(
            originX + column * cellSize + inset,
            originY + row * cellSize + inset,
            Math.Max(1, cellSize - inset * 2),
            Math.Max(1, cellSize - inset * 2));
    }

    private GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
    {
        float diameter = radius * 2;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

}
