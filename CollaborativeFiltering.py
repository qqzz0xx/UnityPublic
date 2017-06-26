import numpy as np
import csv
import math

from itertools import islice

#欧几里德距离
def CompDist(myUserData, otherUserData, ratingsMin=0.5, ratingsMax=5.0):
    num = 0
    otherval = (ratingsMin + ratingsMax) * 0.5
    for mymv in myUserData:
        myval = float(mymv["rating"]) 
        for other in otherUserData:
            if mymv["movieId"] == other["movieId"]:
                 otherval = float(other["rating"])
                 
            num += (myval - otherval) * (myval - otherval)

    return math.sqrt(num)
    
def ReadCSV(path):
    with open(path, "r") as f:
        reader = csv.DictReader(f)
        rows = [row for row in reader]    
        return rows

def BuildUserData(ratingsData):
    userData = {}
    for data in ratingsData:
        if not data["userId"] in userData:
            userData[data["userId"]] = []
        userData[data["userId"]].append(data)
    return userData
def BuildUserMovieData(ratingsData):
    userMovieData = {}
    for data in ratingsData:
        if not data["movieId"] in userMovieData:
            userMovieData[data["movieId"]] = []
        userMovieData[data["movieId"]].append(data)
    return userMovieData    


def FindNeighbor(ratingsData, userId, top):
    userData = BuildUserData(ratingsData)
    userMovieData = BuildUserMovieData(ratingsData)

    myUserData = userData[userId]
    neighborUser = [];

    for one in myUserData:
        mvId = one["movieId"]
        oneMovieData = userMovieData[mvId]
        for other in oneMovieData:
            if not other["userId"] in neighborUser and not other["userId"] == userId:
                neighborUser.append(other["userId"])
       
    neighborUserVal = []
    for other in neighborUser:
        val = CompDist(myUserData, userData[other])
        neighborUserVal.append((other, val))
    
    neighborUserVal.sort(key=lambda x : x[1])
    
    neighborUserData = {}
    for neighbor in neighborUserVal[:top]:
        data = userData[neighbor[0]]
        neighborUserData[neighbor[0]] = data
    return neighborUserData
    

if __name__ == '__main__':
    movies = r"d:\ZZZ\TestData\ml-latest-small\movies.csv"
    ratings = r"d:\ZZZ\TestData\ml-latest-small\ratings.csv"
    moviesData = ReadCSV(movies)
    ratingsData = ReadCSV(ratings)

    
      
    neighborUserData = FindNeighbor(ratingsData, "2", 3)
    mvid = []
    for key in neighborUserData:
        for data in neighborUserData[key]:
            if not data["movieId"] in mvid:
                mvid.append(data["movieId"])
    
    mvlist = [mv for mv in moviesData if mv["movieId"] in mvid]
    for mv in mvlist:
        print(mv["title"])
        
    
    
                    
