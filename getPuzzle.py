from password import pword


uname = "SanderGott"

import urllib.request
import re
import os
import json

file_path = "puzzle.json"

url = "http://www.hacker.org/cross/index.php/"
url = url + "?name=" + uname + "&password=" + pword

def getPuzzle():
    with urllib.request.urlopen(url) as response:
        html = response.read().decode('utf-8')

        # Find line that start with "<script>var boardinit"

        puzzle = re.search(r'<script>var boardinit = (.+?);</script>', html).group()

        boardinit_match = re.search(r'boardinit = "(.*?)"', puzzle)
        level_match = re.search(r'level = (\d+)', puzzle)

        # Extract values if they are found
        boardinit = boardinit_match.group(1) if boardinit_match else None
        level = int(level_match.group(1)) if level_match else None

        print("boardinit:", boardinit)  # Output: "1001,1220"
        print("level:", level)          # Output: 10

        if boardinit and level:
            # Check if the file exists; if not, initialize it with an empty dictionary.
            if not os.path.exists(file_path):
                data = {}

            else:
                with open(file_path, "r") as f:
                    data = json.load(f)

            if str(level) not in data:
                # Update or add the data for the specified level.
                data[level] = {
                    "boardStr": boardinit,
                    "solution": "",
                    "posted": False
                }

                # Write the updated data back to the file.
                with open(file_path, "w") as f:
                    json.dump(data, f, indent=4)

        else:
            return None


def postPuzzle(level):
    with open("puzzle.json", "r") as f:
        data = json.load(f)

    if str(level) in data:
        solution = data[str(level)]
        boardStr = solution["boardStr"]
        solution = solution["solution"]

        url = "http://www.hacker.org/cross/index.php/"
        url = url + "?name=" + uname + "&password=" + pword + "&lvl=" + str(level) + "&sol=" + solution

        with urllib.request.urlopen(url) as response:
            html = response.read().decode('utf-8')

            if "Congratulations" in html:
                with open(file_path, "r") as f:
                    data = json.load(f)

                

                with open(file_path, "w") as f:
                    json.dump(data, f, indent=4)

            return html

        return None


import subprocess

if __name__ == "__main__":
    #getPuzzle()
    postPuzzle(13)

    #executable = "HackerFlips/bin/Debug/net8.0/HackerFlips.exe"
    executable = "FlipsGauus/bin/Debug/net8.0/FlipsGauus.exe"

    start = 108

    while True:
        getPuzzle()

        # Run the .exe file
        result = subprocess.run([executable], capture_output=True, text=True)

        # Print the output
        print(result.stdout)



        #inp = input("Press enter to post puzzle " + str(start) + " or type 'exit' to quit: ")
        #if inp == "exit":
        #    break
        

        postPuzzle(start)
        start += 1
    

    