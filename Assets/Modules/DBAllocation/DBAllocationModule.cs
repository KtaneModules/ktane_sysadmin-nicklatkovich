using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DBAllocationModule : MonoBehaviour {
	private const int NODES_COUNT = 100;
	private const int LINES_COUNT = 14;
	private const int MAX_COMMAND_LENGTH = 27;
	private const string INPUT_PREFIX = "<color=#2f2> > ";
	private const string NUMBER_COLOR = "#44f";

	private static int moduleIdCounter = 1;

	public TextMesh TextField;
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;

	private bool _solved = false;
	public bool solved { get { return _solved; } }

	private int _moduleId = 0;
	public int moduleId { get { return _moduleId; } }

	private int _recoveredNodesCount = 0;
	public int recoveredNodesCount { get { return _recoveredNodesCount; } }

	private int _startingTimeInMinutes = 0;
	public int startingTimeInMinutes { get { return _startingTimeInMinutes; } }

	private class Server {
		public readonly int id;
		public readonly int size;
		Vector2Int? allocation = null;
		public Server(int id, int size) {
			this.id = id;
			this.size = size;
		}
	}

	private bool typing = false;
	private bool selected = false;
	private bool shouldUpdateText = true;
	private int linePointer = LINES_COUNT - 1;
	private string command = "";
	private Server[] servers;
	private string[] text = new string[LINES_COUNT];
	private HashSet<int> serverIds = new HashSet<int>();
	private HashSet<int> damagedNodeIds = new HashSet<int>();
	private HashSet<int> recoveredNodeIds = new HashSet<int>();
	private Dictionary<int, Vector2Int> errorCodes = new Dictionary<int, Vector2Int>();

	private void Start() {
		_moduleId = moduleIdCounter++;
		KMBombModule module = GetComponent<KMBombModule>();
		module.OnActivate += Activate;
		KMSelectable selfSelectable = GetComponent<KMSelectable>();
		selfSelectable.OnFocus += () => selected = true;
		selfSelectable.OnDefocus += () => selected = false;
	}

	private void Activate() {
		_startingTimeInMinutes = Mathf.FloorToInt(BombInfo.GetTime() / 60f);
		for (int i = 0; i < 10; i++) serverIds.Add(Random.Range(0, NODES_COUNT));
		servers = new Server[serverIds.Count];
		int j = 0;
		foreach (int serverId in serverIds) servers[j++] = new Server(serverId, Random.Range(1, 11));
		for (int i = 0; i < 10; i++) Damage();
		StartCoroutine(Virus());
		typing = true;
		shouldUpdateText = true;
	}

	private void Update() {
		if (shouldUpdateText) UpdateText();
	}

	private void OnGUI() {
		if (!selected) return;
		Event e = Event.current;
		if (e.type != EventType.KeyDown) return;
		if (ProcessKey(e.keyCode)) shouldUpdateText = true;
	}

	private IEnumerator Virus() {
		while (!solved) {
			Damage();
			yield return new WaitForSeconds(Random.Range(0f, 10f));
		}
	}

	private void Damage() {
		int damagedNodeId = Random.Range(0, 3) == 0 ? serverIds.PickRandom() : Random.Range(0, NODES_COUNT);
		int totalDamagesCount = damagedNodeIds.Count + recoveredNodesCount;
		if (!damagedNodeIds.Contains(damagedNodeId) && Random.Range(-1, totalDamagesCount) < 0) {
			damagedNodeIds.Add(damagedNodeId);
			errorCodes[damagedNodeId] = ErrorCodes.RandomErrorCodeIndex();
		}
	}

	private bool ProcessKey(KeyCode key) {
		if (!typing) return false;
		if (key == KeyCode.Return || key == KeyCode.KeypadEnter) {
			if (command.Length < MAX_COMMAND_LENGTH) {
				text[linePointer] = text[linePointer].Remove(text[linePointer].Length - 9, 1);
			}
			linePointer = (linePointer + 1) % LINES_COUNT;
			typing = false;
			if (!Regex.IsMatch(command, "^ *$")) {
				StartCoroutine(ProcessCommand());
				return false;
			}
			typing = true;
			command = "";
			return true;
		}
		if (key == KeyCode.Backspace && command.Length > 0) {
			command = command.Remove(command.Length - 1);
			return true;
		}
		if (command.Length >= MAX_COMMAND_LENGTH) return false;
		if (key == KeyCode.Space) {
			command += " ";
			return true;
		}
		if (key >= KeyCode.A && key <= KeyCode.Z) {
			string add = key.ToString();
			if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) add = add.ToLower();
			command += add;
			return true;
		}
		if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9) key = KeyCode.Alpha0 + (key - KeyCode.Keypad0);
		if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) {
			command += key - KeyCode.Alpha0;
			return true;
		}
		return false;
	}

	private void EndCommandProcessing() {
		command = "";
		typing = true;
		shouldUpdateText = true;
	}

	private IEnumerator ProcessCommand() {
		command = command.Trim().ToLower();
		if (command == "clear") {
			for (int i = 0; i < LINES_COUNT; i++) text[i] = "";
			EndCommandProcessing();
			yield break;
		}
		if (command == "status") {
			WriteLine(string.Format("Allocated servers: <color={0}>0</color>/<color={0}>7</color>", NUMBER_COLOR));
			WriteLine(string.Format("Damaged nodes: <color={0}>{1}</color>", NUMBER_COLOR, damagedNodeIds.Count));
			WriteLine(string.Format("Recovered nodes: <color={0}>{1}</color>", NUMBER_COLOR, recoveredNodesCount));
			EndCommandProcessing();
			yield break;
		}
		if (command == "serverlist") {
			foreach (Server server in servers) {
				if (damagedNodeIds.Contains(server.id)) {
					WriteLine("<color=red>ERROR</color>: unable connect to server");
					WriteLine(string.Format("Node <color=yellow>#{0}</color> damaged", server.id));
					EndCommandProcessing();
					yield break;
				}
				WriteLine(string.Format("Server <color=yellow>{0}</color>: <color={1}>{2}</color> TB",
					("#" + server.id).PadLeft(3), NUMBER_COLOR, server.size.ToString().PadLeft(2)));
			}
			EndCommandProcessing();
			yield break;
		}
		if (Regex.IsMatch(command, "^debug( |$)")) {
			string[] args = command.Split(' ').Where((s) => s.Length > 0).ToArray();
			if (args.Length != 2) {
				WriteLine("<color=red>ERROR</color>: invalid args count");
				EndCommandProcessing();
				yield break;
			}
			if (!Regex.IsMatch(args[1], @"^(0|[1-9]\d?)$")) {
				WriteLine("<color=red>ERROR</color>: invalid 1st arg");
				WriteLine(string.Format("expected number in range [<color={0}>0</color>-<color={0}>99</color>]",
					NUMBER_COLOR));
				EndCommandProcessing();
				yield break;
			}
			int nodeId = int.Parse(args[1]);
			text[linePointer] = "Debugging...";
			yield return Loader(.2f, 16, 9);
			if (damagedNodeIds.Contains(nodeId)) {
				WriteLine(string.Format("Node <color=yellow>#{0}</color> damaged", nodeId));
				WriteLine(string.Format("Error code: <color=red>{0}</color>",
					ErrorCodes.ErrorCode(errorCodes[nodeId])));
			} else WriteLine(string.Format("Node <color=yellow>#{0}</color> operational", nodeId));
			EndCommandProcessing();
			yield break;
		}
		if (Regex.IsMatch(command, "^recover( |$)")) {
			string[] args = command.Split(' ').Where((s) => s.Length > 0).ToArray();
			if (args.Length != 3) {
				WriteLine("<color=red>ERROR</color>: invalid args count");
				EndCommandProcessing();
				yield break;
			}
			if (!Regex.IsMatch(args[1], @"^(0|[1-9]\d?)$")) {
				WriteLine("<color=red>ERROR</color>: invalid 1st arg");
				WriteLine(string.Format("expected number in range [<color={0}>0</color>-<color={0}>99</color>]",
					NUMBER_COLOR));
				EndCommandProcessing();
				yield break;
			}
			int nodeId = int.Parse(args[1]);
			if (!damagedNodeIds.Contains(nodeId)) {
				Debug.LogFormat("[DB Allocation #{0}] Trying to recover operational node #{1}", moduleId, nodeId);
				text[linePointer] = "Recovering...";
				yield return Loader(.2f, 32, 10);
				WriteLine(string.Format("<color=red>ERROR</color> Node <color=yellow>#{0}</color> not damaged",
					nodeId));
				yield return HandleStrike();
				EndCommandProcessing();
				yield break;
			}
			string validRecoveryCode = ErrorCodes.ValidRecoveryCode(errorCodes[nodeId], this);
			if (validRecoveryCode != args[2].ToUpper()) {
				Debug.LogFormat("[DB Allocation #{0}] Invalid recovery code for #{1}. Entered: {2}. Expected: {3}",
					moduleId, nodeId, args[2].ToUpper(), validRecoveryCode);
				text[linePointer] = "Recovering...";
				yield return Loader(.2f, 32, 10);
				WriteLine("<color=red>ERROR</color> Invalid recovery code");
				yield return HandleStrike();
				EndCommandProcessing();
				yield break;
			}
			Debug.LogFormat("[DB Allocation #{0}] Recovering code {1} is valid for node #{2}", moduleId,
				validRecoveryCode, nodeId);
			text[linePointer] = "Recovering...";
			yield return Loader(.2f, 32, 10);
			WriteLine(string.Format("Node <color=yellow>#{0}</color> recovered", nodeId));
			damagedNodeIds.Remove(nodeId);
			recoveredNodeIds.Add(nodeId);
			_recoveredNodesCount += 1;
			EndCommandProcessing();
			yield break;
		}
		WriteLine("<color=red>ERROR</color>: Unknown command");
		EndCommandProcessing();
	}

	private IEnumerator HandleStrike() {
		text[linePointer] = "Reverting all changes...";
		yield return Loader(.2f, 48, 21);
		text[linePointer] = "<color=red>STRIKE</color>...";
		yield return Loader(.2f, 8, 25);
		BombModule.HandleStrike();
		for (int i = 0; i < LINES_COUNT; i++) text[i] = "";
	}

	private void WriteLine(string str) {
		text[linePointer++] = str;
		if (linePointer >= LINES_COUNT) linePointer = 0;
		shouldUpdateText = true;
	}

	private IEnumerator Loader(float interval, int steps, int pos) {
		for (int i = 0; i < steps; i++) {
			text[linePointer] = text[linePointer].Remove(pos) + Enumerable.Range(0, 3).Select((j) => (
				i % 4 <= j ? ' ' : '.'
			)).Join("");
			shouldUpdateText = true;
			yield return new WaitForSeconds(interval);
		}
	}

	private void UpdateText() {
		if (typing) {
			text[linePointer] = new string[] {
				INPUT_PREFIX,
				command,
				command.Length < MAX_COMMAND_LENGTH ? "_" : "",
				"</color>",
			}.Join("");
		}
		TextField.text = Enumerable.Range(linePointer + 1, LINES_COUNT).Select((i) => (
			text[i % LINES_COUNT]
		)).Join("\n");
		shouldUpdateText = false;
	}
}
