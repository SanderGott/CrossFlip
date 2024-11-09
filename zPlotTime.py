import requests
from bs4 import BeautifulSoup
import pandas as pd
import matplotlib.pyplot as plt

# URL to retrieve data
url = "https://www.hacker.org/util/rawhistdata.php?userid=50041&game=cross"

print("1")
# Step 1: Retrieve data from the URL
response = requests.get(url)
if response.status_code == 200:
    html_data = response.text
else:
    raise Exception(f"Failed to retrieve data. Status code: {response.status_code}")

print("1.5")
# Step 2: Parse the HTML to extract timestamps and levels
soup = BeautifulSoup(html_data, 'html.parser')
rows = soup.find_all('tr')
print("2")
# Extract timestamp and level from each row
data = []
for row in rows:
    cells = row.find_all('td')
    if len(cells) == 2:  # Ensure there are exactly two columns (timestamp and level)
        timestamp = cells[0].text.strip()
        level = cells[1].text.strip()
        data.append((timestamp, level))

print("3")
# Convert extracted data into a DataFrame
df = pd.DataFrame(data, columns=["timestamp", "level"])
df['timestamp'] = pd.to_datetime(df['timestamp'])
df['level'] = pd.to_numeric(df['level'])

# Step 3: Calculate the time difference between levels
df['time_diff'] = df['timestamp'].diff(-1).abs()
print("4")
# Filter for time differences less than or equal to 15 minutes
filtered_df = df[df['time_diff'] <= pd.Timedelta(minutes=15)].reset_index(drop=True)


# Check if next time_diff is greater than 3 minutes + current time_diff
# If so, remove the current time_diff
for i in range(len(filtered_df) - 1):
    print("a")
    if filtered_df['time_diff'][i + 1] > pd.Timedelta(minutes=3) + filtered_df['time_diff'][i]:
        filtered_df['time_diff'][i] = pd.NaT
    
        

    

# Step 4: Plot the data
plt.figure(figsize=(10, 5))
plt.plot(filtered_df['level'][:-1], filtered_df['time_diff'][:-1].dt.total_seconds() / 60, marker='o', linestyle='-')
plt.xlabel('Level')
plt.ylabel('Time Taken (minutes)')
plt.title('Time Taken Between Levels (Filtered for ≤15 Minutes)')
# cut off x at 15
x1, x2 = plt.xlim()
plt.xlim(15, x2)
plt.grid(True)
plt.show()
