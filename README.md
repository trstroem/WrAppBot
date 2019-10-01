# WrAppBot

This repo is for the Microsoft Hack 2019 - project WrApp.

This project aims to create a chatbot that uses input from users to generate a storyline - like a simple fairy tale. It tries to use cognitive services (and possibly other services, like SAP HANA Text Analysis) to "understand" what the users (target group is children aged 5-15) express, and then build on their comments to drive the storyline forward.

An ideal example would look something like this:

WrAppBot: Hi! What should your story be about? <br>
User: I want to write about foxes! <br>
WrAppBot: Foxes are so cool! What is the name of the fox? <br>
User: Let's call him Billy <br>
WrAppBot: Once upon a time, there was a fox called Billy. He lived in a big forest. One morning he woke up and heard a loud noice! What do you think happened? <br>
User: It was an angry bear!! <br>
WrAppBot: Cool! I love angry bears! What's the name of the bear? <br>

...

For this to happen, we need two things: 1) a decent way to extract nouns/verbs/names/other word classes from the text (plus some sentiment analysis), and 2) a storyline generator to build on the extracted info.

Currently (October 2019) the repo contains the MS Bot logic - which has calls to two services: an Azure Cognitive Services sentiment analysis endpoint, and an SAP Cloud Platform trial HANA DB with a simple HANA Text Analysis xsjs service (this HANA DB is deleted as per October 1, 2019, but could easily be re-created if/when needed). The advantage of the HANA TA service is that it easily extracts a variety of word classes (nouns, verbd, adjectives...), something Azure CS seems to lack for the time being.
