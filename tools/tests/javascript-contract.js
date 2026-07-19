"use strict";

const lexicalToken = /"(?:\\[\s\S]|[^"\\])*"|'(?:\\[\s\S]|[^'\\])*'|`(?:\\[\s\S]|[^`\\])*`|\/\/[^\r\n]*|\/\*[\s\S]*?\*\//g;
const blankToken = token => token.replace(/[^\r\n]/g, " ");
const maskNonCode = value => value.replace(lexicalToken, blankToken);
const stripComments = value => value.replace(lexicalToken,
  token => token.startsWith("//") || token.startsWith("/*") ? blankToken(token) : token);

function matchingDelimiter(masked, openIndex, open, close) {
  let depth = 0;
  for (let index = openIndex; index < masked.length; index += 1) {
    if (masked[index] === open) depth += 1;
    if (masked[index] === close) depth -= 1;
    if (depth === 0) return index;
  }
  throw new Error(`Unmatched ${open} at ${openIndex}.`);
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function escapedCallee(callee) {
  return callee.split(".").map(escapeRegExp).join("\\s*\\.\\s*");
}

function findCalls(value, callee, { awaited = true } = {}) {
  const masked = maskNonCode(value);
  const prefix = awaited ? "\\bawait\\s+" : "\\b";
  const pattern = new RegExp(`${prefix}${escapedCallee(callee)}\\s*\\(`, "g");
  const calls = [];
  let match;
  while ((match = pattern.exec(masked)) !== null) {
    if (!awaited && /\bfunction\s*$/.test(masked.slice(Math.max(0, match.index - 32), match.index))) {
      continue;
    }
    const openIndex = masked.indexOf("(", match.index);
    const closeIndex = matchingDelimiter(masked, openIndex, "(", ")");
    calls.push({
      start: match.index,
      end: closeIndex + 1,
      text: stripComments(value.slice(match.index, closeIndex + 1)),
    });
    pattern.lastIndex = closeIndex + 1;
  }
  return calls;
}

function extractFunction(value, name) {
  const masked = maskNonCode(value);
  const pattern = new RegExp(`\\b(?:async\\s+)?function\\s+${escapeRegExp(name)}\\s*\\(`, "g");
  const matches = [...masked.matchAll(pattern)];
  if (matches.length !== 1) throw new Error(`Expected exactly one function ${name}; found ${matches.length}.`);
  const blockOpen = masked.indexOf("{", matches[0].index);
  const blockClose = matchingDelimiter(masked, blockOpen, "{", "}");
  return value.slice(matches[0].index, blockClose + 1);
}

function extractPropertyFunction(value, name) {
  const masked = maskNonCode(value);
  const pattern = new RegExp(`\\b${escapeRegExp(name)}\\s*:\\s*function\\s*\\(`, "g");
  const matches = [...masked.matchAll(pattern)];
  if (matches.length !== 1) {
    throw new Error(`Expected exactly one function property ${name}; found ${matches.length}.`);
  }
  const blockOpen = masked.indexOf("{", matches[0].index);
  const blockClose = matchingDelimiter(masked, blockOpen, "{", "}");
  return value.slice(matches[0].index, blockClose + 1);
}

function propertyFunctionNames(value, prefix) {
  const masked = maskNonCode(value);
  const pattern = new RegExp(`\\b(${escapeRegExp(prefix)}[A-Za-z0-9_$]*)\\s*:\\s*function\\s*\\(`, "g");
  return [...masked.matchAll(pattern)].map(match => match[1]);
}

function quotedValues(value) {
  const values = [];
  value.replace(lexicalToken, token => {
    if (token.startsWith('"') || token.startsWith("'")) {
      const body = token.slice(1, -1)
        .replace(/\\([\\'"nrt])/g, (_, escaped) => ({ n: "\n", r: "\r", t: "\t" }[escaped] || escaped));
      values.push(body);
    }
    return token;
  });
  return values;
}

function lastQuotedValue(call) {
  return quotedValues(call.text).at(-1);
}

function canonicalCode(value) {
  return stripComments(value).replace(/\s+/g, " ").trim();
}

module.exports = {
  canonicalCode,
  extractFunction,
  extractPropertyFunction,
  findCalls,
  lastQuotedValue,
  maskNonCode,
  matchingDelimiter,
  propertyFunctionNames,
  quotedValues,
  stripComments,
};
