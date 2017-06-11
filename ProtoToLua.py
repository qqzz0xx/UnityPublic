import sys
import os.path
import os

Token_Enum = "enum"
Token_OPEN_BRACKET = "{"
Token_CLOSE_BRACKET = "}"
Token_EQUAL = "="
Token_ITEM_END = ";"

def MatchToken(text, offset, length, token):
    tokenLen = len(token)
    while(offset < length):
        if (str.isspace(text[offset])):
            offset += 1
            continue

        if (length - offset < tokenLen ):
            return -1
        if (text[offset:offset+tokenLen].lower() == token):
            return 0, offset, offset+tokenLen
        offset += 1

def MatchTokenString(text, offset, length):
    iok = -1
    ibegin = 0
    iend = 0
    while(offset < length):
        if (str.isspace(text[offset])):
            offset += 1
            continue
        else:
            ibegin = offset
            iok = 0
            break

    while(offset < length):
        if (not str.isspace(text[offset])):
            offset += 1
            continue
        else:
            iend = offset - 1
            break

    return iok, ibegin, iend

def MatchTokenNum(text, offset, length):
    iok = -1
    ibegin = 0
    iend = 0
    c = ''
    while(offset < length):
        c = text[offset]
        if (not str.isdigit(c)):
            offset += 1
            continue
        else:
            ibegin = offset
            iok = 0
            break

    while(offset < length):
        c = text[offset]
        if (str.isdigit(c)):
            offset += 1
            continue
        else:
            iend = offset - 1
            break

    return iok, ibegin, iend          


def MatchEnumItem(text, offset, length):
    if (offset >= length):
        return -1

    key = None
    val = None
    iok, ibegin, iend = MatchTokenString(text, offset, length)
    if (iok != 0):
        return iok
    key = text[ibegin:iend+1]
    iItemBegin = ibegin

    iok, ibegin, iend = MatchToken(text, iend+1, length, Token_EQUAL)
    if (iok != 0):
        return iok

    iok, ibegin, iend = MatchTokenNum(text, iend+1, length)
    if (iok != 0):
        return iok
    val = text[ibegin:iend+1]

    iok, ibegin, iend = MatchToken(text, iend+1, length, Token_ITEM_END)
    if (iok != 0):
        return iok

    iItemEnd = iend

    return 0, iItemBegin, iItemEnd, key, val

def MatchEnumObject(text, offset, length):
    iok = -1;
    iok, ibegin, iend = MatchToken(text, offset, length, Token_Enum)
    if (iok != 0):
        return iok
    iok, ibegin, iend = MatchToken(text, iend+1, length, Token_OPEN_BRACKET)
    if (iok != 0):
        return iok
    iItemsBegin = iend + 1
    iok, ibegin, iend = MatchToken(text, iend+1, length, Token_CLOSE_BRACKET)
    if (iok != 0):
        return iok
    iItemsEnd = ibegin - 1

    while(iItemsBegin < iItemsEnd + 1):
        iok, ibegin, iend, key, val = MatchEnumItem(text, iItemsBegin, iItemsEnd + 1)
        if (iok != 0):
            return iok
        iItemsBegin = iend + 1;
        print("key = %s, value = %s" % (key, val))


def PaserProto(filename):
    with open(filename,"r", encoding="UTF8") as f:
        text = f.read()
        MatchEnumObject(text, 0, len(text))

if __name__ == '__main__':
    PaserProto("CsMacro.proto")
        
