public interface IPlayer
{
	int id { get; }

	void StartTurn();
	void Tick();
	void EndTurn();


	void AddUnit(HexUnit hexUnit);
}
