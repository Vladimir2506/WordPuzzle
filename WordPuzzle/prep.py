import os
import pdb

class Node(object):

    def __init__(self, name=None, value=None):

        self.name = name
        self.value = value
        self.left = None
        self.right = None

class HuffmanTree(object):

    def __init__(self, char_weights):

        self.a = [Node(k, v) for k, v in char_weights.items()]

        while len(self.a) != 1:
            self.a.sort(key=lambda node:node.value, reverse=True)
            c = Node(value=(self.a[-1].value + self.a[-2].value))
            c.left = self.a.pop(-1)
            c.right = self.a.pop(-1)
            self.a.append(c)
        
        self.root = self.a[0]
        self.b = [_ for _ in range(16)]

    def pre(self, tree, length, encode_dict):
        
        node = tree
        if not node: return
        elif node.name:
            code = ''.join([str(_) for _ in self.b[0:length]])
            if len(code) < 16: code = code + '0' * (16 - len(code))
            unsigned_code = int(bytes(code, encoding='utf-8'), base=2)
            encode_dict[node.name] = unsigned_code
        else:
            self.b[length] = 0
            self.pre(node.left, length + 1, encode_dict)
            self.b[length] = 1
            self.pre(node.right, length + 1, encode_dict)

    def get_code(self, encode_dict):
        
        self.pre(self.root, 0, encode_dict)

def main():

    word_dict = {}
    encode_dict = {}
    with open('THUOCL_poem.txt', 'r', encoding='utf-8') as word_base_file:

        for line in word_base_file.readlines():
            
            for c in line:
                if c == '\t' or c == '\n':
                    continue
                if c in word_dict.keys():
                    word_dict[c] += 1
                else:
                    word_dict[c] = 1
    
    tree = HuffmanTree(word_dict)
    tree.get_code(encode_dict)
    
    with open('LUT.txt', 'w', encoding='utf-8') as lut_file:

        for k, v in encode_dict.items():

            lut_file.write(k + ':' + str(v) + '\n')

if __name__ == '__main__':
    main()