namespace LeaderboardBackend.Jobs.Core;

internal class ConsoleMenu<T>
{
	private readonly string _header;
	private readonly List<T> _items;

	public ConsoleMenu(string header, List<T> items)
	{
		_header = header;
		_items = items;
	}

	public T? Choose()
	{
		int choice = 0;

		while (true)
		{
			WriteMenu(choice);

			switch (Console.ReadKey().Key)
			{
				case ConsoleKey.Enter:
				{
					return _items[choice];
				}
				case ConsoleKey.UpArrow:
				{
					if (choice++ == _items.Count)
					{
						choice = 0;
					}

					continue;
				}
				case ConsoleKey.DownArrow:
				{
					if (choice-- == -1)
					{
						choice = _items.Count - 1;
					}

					continue;
				}
				case ConsoleKey.Q:
				{
					return default;
				}
			}
		}
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
