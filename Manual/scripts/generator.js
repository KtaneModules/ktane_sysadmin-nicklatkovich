const COLUMNS_COUNT = 10;
const ROWS_COUNT = 30;
const ERROR_CODE_SIZE = 4;
const MAX_FIX_CODE_LENGTH = 16;
const LETTERS = "TFBIKRMSE";

const firstLetter = "A".charCodeAt(0);
const lettersCount = "Z".charCodeAt(0) - firstLetter + 1;

const allCodes = new Set();

const data = new Array(ROWS_COUNT).fill(0).map(() => new Array(COLUMNS_COUNT).fill(0).map(() => {
	const bin = (Math.floor(Math.random() * 31) + 1).toString(2).padStart(4, '0');
	const result = new Array(ERROR_CODE_SIZE).fill(0).map((_, i) => {
		if (bin[i] === "1") return Math.floor(Math.random() * 10);
		return String.fromCharCode(firstLetter + Math.floor(Math.random() * lettersCount));
	}).join("");
	if (allCodes.has(result)) throw new Error("Duplicate");
	allCodes.add(result);
	return result;
}));

const unusedLetters = new Set(LETTERS.split(""));

const lastColumn = new Array(ROWS_COUNT).fill(0).map(() => {
	if (Math.random() < .5) {
		const result = LETTERS[Math.floor(Math.random() * LETTERS.length)];
		unusedLetters.delete(result);
		return result;
	}
	const bin = (Math.floor(Math.random() * 3)).toString(2) + 1;
	const result = [
		bin[0] === "1" ? LETTERS[Math.floor(Math.random() * LETTERS.length)] : Math.floor(Math.random() * 9) + 1,
		Math.random() > .3 ? "+" : "-",
		bin[1] === "1" ? LETTERS[Math.floor(Math.random() * LETTERS.length)] : Math.floor(Math.random() * 9) + 1,
	].join(" ");
	unusedLetters.delete(result[0]);
	unusedLetters.delete(result[4]);
	return result;
});

if (unusedLetters.size > 0) throw new Error("Not used letter");

for (let i = 0; i < 10; i++) {
	for (let j = 0; j < COLUMNS_COUNT; j++) {
		const count = new Array(ROWS_COUNT).fill(0).map((_, k) => data[k][j].split("")).filter((s) => (
			s.some((c) => c == String.fromCharCode(i + '0'.charCodeAt(0)))
		)).length;
		if (count == 0 || count > MAX_FIX_CODE_LENGTH) throw new Error("Invalid fix code size");
	}
}

console.log("const data = [");
for (const row of data) console.log("    [" + row.map((s) => `"${s}"`).join(", ") + "],");
console.log("];");

console.log("const lastColumn = [");
for (const row of lastColumn) console.log("    \"" + row + "\",");
console.log("];")
