namespace LeaderboardBackend.Jobs.Core;

internal class ConsoleMenu<T>
{
	private readonly string _header;
	private readonly List<T> _items;

	public ConsoleMenu(string header, List<T> items)
	{
		_items = items;
		_header = header;
	}

	public T? Choose()
	{
		int choice = 0;

		ConsoleKeyInfo keyInfo;

		do
		{
			WriteMenu(choice);
			keyInfo = Console.ReadKey();

			switch (keyInfo.Key)
			{
				case ConsoleKey.Q:
					{
						return default;
					}

				case ConsoleKey.UpArrow:
					{
						choice++;
						if (choice == _items.Count)
						{
							choice = 0;
						}

						break;
					}

				case ConsoleKey.DownArrow:
					{
						choice--;
						if (choice == -1)
						{
							choice = _items.Count - 1;
						}

						break;
					}
			}
		} while (keyInfo.Key != ConsoleKey.Enter);

		return _items.ElementAt(choice);
	}

	private void WriteMenu(int index)
	{
		Console.Clear();

		Console.WriteLine(
			$"{_header}\n\n" +
			$"Controls\n" +
			$"    up, down: change selection\n" +
			$"    enter:    choose item\n" +
			$"    q:        quit\n");

		for (int i = 0; i < _items.Count; i++)
		{
			T item = _items[i];
			char prefix = i == index ? '>' : ' ';
			Console.WriteLine($"{prefix}{item?.ToString()}");
		}
	}
}
