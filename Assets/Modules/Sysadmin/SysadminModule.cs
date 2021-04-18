using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SysadminModule : MonoBehaviour {
	private const int NODES_COUNT = 100;
	private const int LINES_COUNT = 14;
	private const int MAX_COMMAND_LENGTH = 27;
	private const int MAX_DAMAGES_COUNT = 20;
	private const string INPUT_PREFIX = "<color=#2f2> > ";
	private const string NUMBER_COLOR = "#44f";

	private static int moduleIdCounter = 1;

	public TextMesh TextField;
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;

	public readonly string TwitchHelpMessage = "\"!{0} command\" - Execute command";

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
		public Vector2Int? allocation = null;
		public Server(int id, int size) {
			this.id = id;
			this.size = size;
		}
	}

	private bool typing = false;
	private bool selected = false;
	private bool shouldUpdateText = true;
	private int allocationsCount = 0;
	private int requiredAllocationsCount = 0;
	private int linePointer = LINES_COUNT - 1;
	private string command = "";
	private Server[] servers;
	private string[] text = new string[LINES_COUNT];
	private HashSet<int> serverIds = new HashSet<int>();
	private HashSet<int> damagedNodeIds = new HashSet<int>();
	private HashSet<int> recoveredNodeIds = new HashSet<int>();
	private Dictionary<int, Vector2Int> errorCodes = new Dictionary<int, Vector2Int>();
	private HashSet<int> allocatedNodes = new HashSet<int>();

	private void Start() {
		_moduleId = moduleIdCounter++;
		KMBombModule module = GetComponent<KMBombModule>();
		module.OnActivate += Activate;
	}

	private void Activate() {
		KMSelectable selfSelectable = GetComponent<KMSelectable>();
		selfSelectable.OnFocus += () => selected = true;
		selfSelectable.OnDefocus += () => selected = false;
		_startingTimeInMinutes = Mathf.FloorToInt(BombInfo.GetTime() / 60f);
		for (int i = 0; i < 10; i++) serverIds.Add(Random.Range(0, NODES_COUNT));
		servers = new Server[serverIds.Count];
		int j = 0;
		foreach (int serverId in serverIds) {
			Server server = new Server(serverId, Random.Range(1, 11));
			servers[j++] = server;
			Debug.LogFormat("[Sysadmin #{0}] Server #{1} created with {2} TB", moduleId, server.id, server.size);
		}
		Server[] sortedServers;
		{
			List<Server> temp = new List<Server>(servers);
			temp.Sort((Server s1, Server s2) => s1.id - s2.id);
			sortedServers = temp.ToArray();
		}
		int maxAllocation = -1;
		for (int i = 0; i < sortedServers.Length; i++) {
			Server s = sortedServers[i];
			if (s.id - s.size > maxAllocation) {
				maxAllocation = s.id;
				requiredAllocationsCount += 1;
			} else if (s.id + s.size < 100 && (
				i == sortedServers.Length - 1 || sortedServers[i + 1].id > s.id + s.size
			)) {
				maxAllocation = s.id + s.size;
				requiredAllocationsCount += 1;
			} else maxAllocation = s.id;
		}
		Debug.LogFormat("[Sysadmin #{0}] Required allocations count: {1}", moduleId, requiredAllocationsCount);
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

	public IEnumerator ProcessTwitchCommand(string command) {
		if (Regex.IsMatch(command, @"^[ a-zA-Z0-9-*/]{1,27}$")) {
			this.command = command;
			UpdateText();
			BeforeCommandProcess();
			if (!Regex.IsMatch(command, "^ *$")) {
				yield return null;
				yield return ProcessCommand();
				command = "";
				yield break;
			}
		}
	}

	private IEnumerator Virus() {
		while (!solved && damagedNodeIds.Count + recoveredNodesCount < MAX_DAMAGES_COUNT) {
			Damage();
			yield return new WaitForSeconds(Random.Range(0f, 20f));
		}
	}

	private void Damage() {
		int damagedNodeId = Random.Range(0, 3) == 0 ? serverIds.PickRandom() : Random.Range(0, NODES_COUNT);
		if (damagedNodeIds.Contains(damagedNodeId)) return;
		if (recoveredNodeIds.Contains(damagedNodeId)) return;
		if (allocatedNodes.Contains(damagedNodeId)) return;
		if (serverIds.Contains(damagedNodeId) && servers.First((s) => s.id == damagedNodeId).allocation != null) return;
		int totalDamagesCount = damagedNodeIds.Count + recoveredNodesCount;
		if (Random.Range(-1, totalDamagesCount) >= 0) return;
		Debug.LogFormat("[Sysadmin #{0}] Node #{1} damaged", moduleId, damagedNodeId);
		damagedNodeIds.Add(damagedNodeId);
		errorCodes[damagedNodeId] = ErrorCodes.RandomErrorCodeIndex();
	}

	private void BeforeCommandProcess() {
		if (command.Length < MAX_COMMAND_LENGTH) {
			text[linePointer] = text[linePointer].Remove(text[linePointer].Length - 9, 1);
		}
		linePointer = (linePointer + 1) % LINES_COUNT;
		typing = false;
	}

	private bool ProcessKey(KeyCode key) {
		if (!typing) return false;
		if (key == KeyCode.Return || key == KeyCode.KeypadEnter) {
			BeforeCommandProcess();
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
		if (
			key == KeyCode.Asterisk
			|| key == KeyCode.KeypadMultiply
			|| (key == KeyCode.Alpha8 && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
		) {
			command += "*";
			return true;
		}
		if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9) key = KeyCode.Alpha0 + (key - KeyCode.Keypad0);
		if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) {
			command += key - KeyCode.Alpha0;
			return true;
		}
		if (key == KeyCode.Slash || key == KeyCode.KeypadDivide) {
			command += '/';
			return true;
		}
		if (key == KeyCode.Minus || key == KeyCode.KeypadMinus) {
			command += '-';
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
		if (command == "sudo rm -rf /*") {
			WriteLine("<color=red>Nice try, susadmin</color>");
			Debug.LogFormat("[Sysadmin #{0}] Susadmin detected!", moduleId);
			BombModule.HandleStrike();
			EndCommandProcessing();
			yield break;
		}
		if (command == "revert") {
			Debug.LogFormat("[Sysadmin #{0}] Reverting command entered", moduleId);
			yield return HandleStrike();
			EndCommandProcessing();
			yield break;
		}
		if (command == "status") {
			WriteLine(string.Format((
				"Allocated servers: <color={0}>{1}</color>/<color={0}>{2}</color>"
			), NUMBER_COLOR, allocationsCount, requiredAllocationsCount));
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
				if (server.allocation == null) {
					WriteLine(
						string.Format(
							"Server <color=yellow>{0}</color>: <color={1}>{2}</color> TB",
							("#" + server.id).PadLeft(3), NUMBER_COLOR, server.size.ToString().PadLeft(2)
						)
					);
				} else {
					Vector2Int allocation = (Vector2Int)server.allocation;
					WriteLine(
						string.Format(
							new string[] {
								"Server <color=yellow>{0}</color>:",
								"<color={1}>{2}</color> TB",
								"<color=yellow>[{3}-{4}]</color>",
							}.Join(" "), ("#" + server.id).PadLeft(3), NUMBER_COLOR, server.size.ToString().PadLeft(2),
							allocation.x, allocation.y
						)
					);
				}
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
				Debug.LogFormat("[Sysadmin #{0}] Trying to recover operational node #{1}", moduleId, nodeId);
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
				Debug.LogFormat("[Sysadmin #{0}] Invalid recovery code for #{1}. Entered: {2}. Expected: {3}",
					moduleId, nodeId, args[2].ToUpper(), validRecoveryCode);
				text[linePointer] = "Recovering...";
				yield return Loader(.2f, 32, 10);
				WriteLine("<color=red>ERROR</color> Invalid recovery code");
				yield return HandleStrike();
				EndCommandProcessing();
				yield break;
			}
			Debug.LogFormat("[Sysadmin #{0}] Recovering code {1} is valid for node #{2}", moduleId,
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
		if (Regex.IsMatch(command, "^allocate( |$)")) {
			string[] args = command.Split(' ').Where((s) => s.Length > 0).ToArray();
			if (args.Length != 3) {
				WriteLine("<color=red>ERROR</color>: invalid args count");
				EndCommandProcessing();
				yield break;
			}
			if (!Regex.IsMatch(args[1], @"^(0|[1-9]\d?)$")) {
				WriteLine("<color=red>ERROR</color>: invalid 1st arg");
				WriteLine(
					string.Format("expected number in range [<color={0}>0</color>-<color={0}>99</color>]", NUMBER_COLOR)
				);
				EndCommandProcessing();
				yield break;
			}
			if (!new string[] { "left", "down", "right", "up" }.Contains(args[2])) {
				WriteLine("<color=red>ERROR</color>: invalid 2st arg");
				EndCommandProcessing();
				yield break;
			}
			int nodeId = int.Parse(args[1]);
			if (!serverIds.Contains(nodeId)) {
				WriteLine(
					string.Format("<color=red>ERROR</color>: server <color=yellow>#{0}</color> not found", nodeId)
				);
				EndCommandProcessing();
				yield break;
			}
			if (damagedNodeIds.Contains(nodeId)) {
				WriteLine("<color=red>ERROR</color>: unable connect to server");
				WriteLine(string.Format("Node <color=yellow>#{0}</color> damaged", nodeId));
				EndCommandProcessing();
				yield break;
			}
			text[linePointer] = "Allocation...";
			yield return Loader(.2f, 16, 10);
			int direction = args[2] == "left" || args[2] == "down" ? -1 : 1;
			Server server = servers.First((s) => s.id == nodeId);
			if (server.allocation != null) {
				WriteLine("<color=red>ERROR</color>: unable allocate");
				WriteLine(string.Format("Server <color=yellow>#{0}</color> already allocated", server.id));
				EndCommandProcessing();
				yield break;
			}
			for (int i = 1; i <= server.size; i++) {
				int storageId = nodeId + i * direction;
				if (storageId < 0 || storageId >= 100) {
					WriteLine("<color=red>ERROR</color>: unable allocate");
					WriteLine("Node id out of range");
					EndCommandProcessing();
					yield break;
				}
				if (damagedNodeIds.Contains(storageId)) {
					WriteLine("<color=red>ERROR</color>: unable allocate");
					WriteLine(string.Format("Node <color=yellow>#{0}</color> damaged", storageId));
					EndCommandProcessing();
					yield break;
				}
				if (serverIds.Contains(storageId)) {
					Debug.LogFormat(
						"[Sysadmin #{0}] Trying to allocate server #{1} to server #{2}", moduleId, storageId, server.id
					);
					WriteLine("<color=red>ERROR</color>: unable allocate");
					WriteLine(string.Format("Node <color=yellow>#{0}</color> is not data storage", storageId));
					yield return HandleStrike();
					EndCommandProcessing();
					yield break;
				}
				if (allocatedNodes.Contains(storageId)) {
					Debug.LogFormat((
						"[Sysadmin #{0}] Trying to allocate allocated node #{1} to server #{2}"
					), moduleId, storageId, server.id);
					WriteLine("<color=red>ERROR</color>: unable allocate");
					WriteLine(string.Format("Node <color=yellow>#{0}</color> already allocated", storageId));
					yield return HandleStrike();
					EndCommandProcessing();
					yield break;
				}
			}
			for (int i = 1; i <= server.size; i++) {
				int storageId = nodeId + i * direction;
				allocatedNodes.Add(storageId);
			}
			Vector2Int allocation = direction == 1 ?
				new Vector2Int(server.id + 1, server.id + server.size) :
				new Vector2Int(server.id - server.size, server.id - 1);
			server.allocation = allocation;
			allocationsCount += 1;
			Debug.LogFormat("[Sysadmin #{0}] Server #{1} allocated {2}", moduleId, server.id, args[2]);
			WriteLine(string.Format(
				"Nodes <color=yellow>[{0}-{1}]</color> allocated", allocation.x, allocation.y, server.id
			));
			EndCommandProcessing();
			yield break;
		}
		if (command == "commit") {
			text[linePointer] = "Committing...";
			yield return Loader(.2f, 48, 10);
			if (allocationsCount >= requiredAllocationsCount) {
				Debug.LogFormat("[Sysadmin #{0}] Committed", moduleId);
				WriteLine("Allocation completed");
				text[linePointer] = "Solving module...";
				yield return Loader(.2f, 16, 14);
				WriteLine("Module solved");
				BombModule.HandlePass();
				shouldUpdateText = true;
				yield break;
			} else {
				Debug.LogFormat("[Sysadmin #{0}] Trying to commit without required allocations", moduleId);
				WriteLine("Allocation not completed");
				yield return HandleStrike();
				EndCommandProcessing();
				yield break;
			}
		}
		WriteLine("<color=red>ERROR</color>: Unknown command");
		EndCommandProcessing();
	}

	private IEnumerator HandleStrike() {
		text[linePointer] = "Reverting all allocations...";
		foreach (Server server in servers) server.allocation = null;
		allocatedNodes = new HashSet<int>();
		allocationsCount = 0;
		for (int i = 0; i < 10; i++) Damage();
		yield return Loader(.2f, 48, 25);
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
