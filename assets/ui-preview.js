const board = document.querySelector("[data-board]");

if (board) {
  const level = Number(board.dataset.level);
  const snakeCells = level === 3
    ? [[2, 2, "tail"], [2, 3, "body"], [2, 4, "head"]]
    : [[6, 3, "tail"], [6, 4, "body"], [6, 5, "head"]];
  const wallCells = [];
  const doorCells = level === 3 ? [[5, 6, "open"]] : [];

  if (level === 3) {
    for (let index = 1; index <= 10; index += 1) {
      wallCells.push([1, index], [10, index]);

      if (index > 1 && index < 10) {
        wallCells.push([index, 1], [index, 10]);
      }
    }

    wallCells.push([4, 4], [4, 5], [6, 7], [7, 7]);
  }

  const snakeDictionary = new Map(
    snakeCells.map(([row, column, part]) => [`${row}-${column}`, part]),
  );
  const wallSet = new Set(wallCells.map(([row, column]) => `${row}-${column}`));
  const doorDictionary = new Map(
    doorCells.map(([row, column, state]) => [`${row}-${column}`, state]),
  );

  for (let row = 1; row <= 10; row += 1) {
    for (let column = 1; column <= 10; column += 1) {
      const key = `${row}-${column}`;
      const cell = document.createElement("div");
      cell.className = "cell";
      cell.setAttribute("aria-hidden", "true");

      if ((row + column) % 2 === 0) {
        cell.classList.add("is-alt");
      }

      if (wallSet.has(key)) {
        const wall = document.createElement("span");
        wall.className = "wall";
        cell.append(wall);
      }

      if (doorDictionary.has(key)) {
        const doorState = doorDictionary.get(key);
        const door = document.createElement("span");
        door.className = `door ${doorState}`;
        cell.append(door);
      }

      if (snakeDictionary.has(key)) {
        const snake = document.createElement("span");
        snake.className = `snake-segment ${snakeDictionary.get(key)}`;
        cell.append(snake);
      }

      board.append(cell);
    }
  }
}
