namespace LeaderboardBackend.Jobs.Core;

internal class ConsoleMenu<T>
{
	private string Header;

	private List<T> Items;

	public ConsoleMenu(string header, List<T> items)
	{
		Items = items;
		Header = header;
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
					return default;

				case ConsoleKey.UpArrow:
					choice++;
					if (choice == Items.Count)
					{
						choice = 0;
					}
					break;

				case ConsoleKey.DownArrow:
					choice--;
					if (choice == -1)
					{
						choice = Items.Count - 1;
					}
					break;
			}
		} while (keyInfo.Key != ConsoleKey.Enter);

		return Items.ElementAt(choice);
	}

	private void WriteMenu(int index)
	{
		Console.Clear();

		Console.WriteLine($"{Header}\n\n" +
		"Controls\n" +
		"	up, down: change selection\n" +
		"	enter:    choose item\n" +
		"	q:        quit\n");

		for (int i = 0; i < Items.Count; i++)
		{
			T item = Items[i];
			char prefix = i == index ? '>' : ' ';
			Console.WriteLine($"{prefix}{item?.ToString()}");
		}
	}
}
