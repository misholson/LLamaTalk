# LLamaTalk
Telegram Chatbot for letting people know when all members of a group have submitted their Learned League questions

Learned League is a daily trivia league at https://learnedleague.com. It's fun to discuss the day's questions with your fellow players, but the most important rule is NO CHEATING.

As such, it is important that in a group chat, no one discusses answers until everyone in the chat has submitted. LLama Talk facilitates that by creating a chat bot for Telegram that will record when you have submitted and notify the group when everyone is done.

## Usage

First, add the chatbot [@LLamaTalkBot](https://t.me/LLamaTalkBot) to your Telegram group.

After that you can use the following commands:
/score N - Submit the number of answers you got correct today (0-6). This will record your correct answers for the day. Once all members of the group have submitted a score, a message will be sent to the group indicating that you are free to discuss the answers.
/status - See who has submitted a score so far today.
/help, /start - Shows a help message.

## Architecture

LLama Talk runs on Azure Functions. To run it, 