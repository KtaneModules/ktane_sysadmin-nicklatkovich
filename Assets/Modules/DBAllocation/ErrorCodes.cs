using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public static class ErrorCodes {
	public const int ERROR_CODE_ROWS_COUNT = 30;
	public const int ERROR_CODE_COLUMN_COUNT = 10;

	public static readonly string[][] data = new string[ERROR_CODE_ROWS_COUNT][] {
        new string[] { "8BP5", "52SA", "G0ST", "6XTT", "7Y79", "0JK0", "FT37", "965B", "Z7C4", "FZY3" },
        new string[] { "6ZP8", "6446", "5L1O", "4D9A", "7T7X", "8IWL", "5841", "56OW", "4AX8", "3WY0" },
        new string[] { "3EAM", "09R0", "KAY6", "2Z3O", "7004", "012J", "1M67", "2G6X", "006K", "437Y" },
        new string[] { "523G", "8MR3", "JW99", "6X6C", "4HZZ", "UMP9", "5UCF", "6S82", "5IM7", "7FX2" },
        new string[] { "V0SR", "2TF5", "4958", "CYB6", "2484", "73T1", "88K2", "4Q1F", "4L73", "93HV" },
        new string[] { "0P3T", "3X80", "3577", "5JVK", "3W31", "20ZK", "1MD1", "3QB2", "5T5U", "6PHO" },
        new string[] { "3O4X", "6N8N", "46UC", "5K27", "0F5V", "74GE", "15UT", "680W", "CMK9", "6WMJ" },
        new string[] { "3D40", "K329", "1J7R", "V5Z9", "8LO4", "5U9H", "975Z", "8S8C", "J27H", "8868" },
        new string[] { "2E3D", "S5Y5", "7Q12", "0CU2", "Y9DD", "E4DR", "6XXS", "6FVU", "547C", "5TVT" },
        new string[] { "6D44", "742X", "D86Q", "S912", "7B2Z", "OEX9", "BB35", "7Y11", "274A", "836C" },
        new string[] { "8567", "PT68", "5RN4", "WIQ2", "0KAY", "0PQ8", "6AG0", "M7S4", "2ROV", "6RO0" },
        new string[] { "391M", "M4NF", "ZUY2", "67W9", "70CW", "6530", "7F4Q", "5PA5", "693M", "6W2F" },
        new string[] { "57T1", "14V2", "0JW4", "8TH7", "WAX1", "6SH6", "7XGD", "250K", "78EU", "4YKZ" },
        new string[] { "86V2", "1X2X", "8NWD", "478A", "823O", "L509", "5BI0", "2267", "TL08", "42J0" },
        new string[] { "4EUE", "4355", "TGZ2", "4Y7Z", "4769", "5NCR", "8A0V", "A9E1", "212K", "1366" },
        new string[] { "1RM6", "6X6O", "1PCO", "7OXG", "X8IZ", "UHW4", "G61J", "01OA", "083J", "2WP1" },
        new string[] { "9JZ8", "4768", "2KRB", "5QNO", "Y2GA", "7A4T", "GBZ2", "XKU3", "U291", "65HQ" },
        new string[] { "89DE", "J9FZ", "3ZJA", "9MLG", "C7FG", "1ZWL", "81J5", "34LZ", "W73I", "SZG1" },
        new string[] { "130Q", "886C", "75ZC", "8IK5", "2C91", "P84G", "7055", "30C2", "FR8H", "8A9S" },
        new string[] { "54JH", "0168", "128J", "11JZ", "22HW", "16UD", "2143", "43CU", "7N47", "7S74" },
        new string[] { "3G15", "0Z4Z", "6K8I", "Y50L", "961B", "1576", "0L60", "IZ47", "9K77", "049I" },
        new string[] { "51P0", "37V1", "HR71", "5J46", "V1VP", "5340", "1ZHJ", "1KRZ", "VF8H", "241X" },
        new string[] { "7QU3", "73ND", "4998", "74FU", "E308", "8V94", "6UMZ", "YBZ6", "2Y99", "103Q" },
        new string[] { "0K4U", "8EN8", "89N2", "0TZ5", "6648", "923B", "8613", "8F28", "1KV4", "1OD3" },
        new string[] { "6ZG0", "0978", "41C5", "8065", "1D21", "23JJ", "57N2", "130U", "30ZB", "P57Y" },
        new string[] { "II6C", "9139", "51YS", "F65N", "0346", "9SC5", "25BI", "I38I", "632R", "8NWB" },
        new string[] { "924U", "22S8", "44HK", "608D", "7353", "24J3", "CW49", "15QF", "JV7M", "2B7G" },
        new string[] { "9988", "UJ98", "44B7", "ZK58", "0C7A", "8Y69", "9MPW", "500G", "634Y", "DY5O" },
        new string[] { "B2J7", "55WZ", "GD9L", "7PT6", "H89I", "7J15", "3516", "YBL4", "664P", "9D50" },
        new string[] { "L149", "629Q", "0ZQD", "8SV1", "N14K", "243X", "956O", "12O0", "7MZN", "1054" },
	};
	public static readonly string[] lastColumn = new string[ERROR_CODE_ROWS_COUNT] {
        "I",
        "S",
        "M",
        "3 + K",
        "F + 5",
        "K",
        "T",
        "7 + E",
        "E",
        "S",
        "T",
        "M",
        "B",
        "M + 2",
        "E",
        "R",
        "T + R",
        "B + 2",
        "4 + M",
        "F",
        "K",
        "8 - S",
        "K - F",
        "B",
        "S",
        "F",
        "T",
        "F + 7",
        "2 + B",
        "B - T",
	};

	public static Vector2Int RandomErrorCodeIndex() {
		return new Vector2Int(Random.Range(0, ERROR_CODE_COLUMN_COUNT), Random.Range(0, ERROR_CODE_ROWS_COUNT));
	}

	public static string ErrorCode(Vector2Int index) {
		return data[index.y][index.x];
	}

	public static string ValidRecoveryCode(Vector2Int errorCodeIndex, SysadminModule module) {
		int value = CalculateValue(errorCodeIndex, module) % 10;
		if (value < 0) value = 10 - value;
		Debug.LogFormat("[Sysadmin #{0}] Recovering value for error code {1}: {2}", module.moduleId,
			data[errorCodeIndex.y][errorCodeIndex.x], value);
		List<string> codes = new List<string>();
		for (int i = 1; i <= ERROR_CODE_ROWS_COUNT; i++) {
			string code = data[(errorCodeIndex.y + i) % ERROR_CODE_ROWS_COUNT][errorCodeIndex.x];
			if (code.Contains((char)(value + '0'))) codes.Add(code);
		}
		return codes.Select((c) => c[0]).Join("");
	}

	public static int CalculateValue(Vector2Int errorCodeIndex, SysadminModule module) {
		string instructions = lastColumn[errorCodeIndex.y];
		Debug.Log(instructions);
		int a = Mutation(instructions[0], module);
		if (instructions.Length == 1) return a;
		int b = Mutation(instructions[4], module);
		return instructions[2] == '+' ? a + b : a - b;
	}

	public static int Mutation(char symbol, SysadminModule module) {
		if (symbol >= '1' && symbol <= '9') return symbol - '0';
		switch (symbol) {
			case 'T': return module.startingTimeInMinutes;
			case 'F': return module.recoveredNodesCount;
			case 'B': return module.BombInfo.GetBatteryCount();
			case 'I': return module.BombInfo.GetIndicators().Count();
			case 'K':
				return module.BombInfo.IsTwoFactorPresent() ?
					module.BombInfo.GetTwoFactorCodes().Select((code) => code % 10).Max() :
					module.BombInfo.GetSolvedModuleIDs().Count();
			case 'R': return Mathf.FloorToInt(module.BombInfo.GetTime() / 60f);
			case 'M': return module.BombInfo.GetModuleIDs().Count;
			case 'S': return module.BombInfo.GetSerialNumberNumbers().Min();
			case 'E': return module.BombInfo.GetStrikes();
			default: throw new UnityException("Invalid symbol");
		}
	}
}
