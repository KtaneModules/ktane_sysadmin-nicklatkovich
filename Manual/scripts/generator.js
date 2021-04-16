const COLUMNS_COUNT = 8;
const ROWS_COUNT = 17;
const ERROR_CODE_SIZE = 4;

const firstLetter = "A".charCodeAt(0);
const lettersCount = "Z".charCodeAt(0) - firstLetter + 1;

const data = new Array(ROWS_COUNT).fill(0).map(() => new Array(COLUMNS_COUNT).fill(0).map(() => (
	new Array(ERROR_CODE_SIZE).fill(0).map(() => {
		if (Math.random() < .5) return Math.floor(Math.random() * 10);
		return String.fromCharCode(firstLetter + Math.floor(Math.random() * lettersCount));
	}).join("")
)));

for (const row of data) console.log(row.join(" "));
