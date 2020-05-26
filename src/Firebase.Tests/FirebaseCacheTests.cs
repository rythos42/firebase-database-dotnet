﻿namespace Firebase.Database.Tests
{
    using Firebase.Database;
    using Firebase.Database.Streaming;
    using Firebase.Database.Tests.Entities;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;

    [TestClass]
    public class FirebaseCacheTests
    {
        [TestMethod]
        public void InitialPushOfEntireDictionaryToEmptyCache()
        {
            // this should simulate first time connection is made and all data is returned in a batch in a form of a dictionary
            var cache = new FirebaseCache<Dinosaur>();
            var dinosaurs = @"{
  ""lambeosaurus"": {
    ""ds"": {
      ""height"" : 2,
      ""length"" : 2,
      ""weight"": 2
    }
  },
  ""stegosaurus"": {
    ""ds"": {
      ""height"" : 3,
      ""length"" : 3,
      ""weight"" : 3
    }
  }
}";

            var entities = cache.PushData("/", dinosaurs).ToList();
            var expectation = new[]
            {
                new FirebaseObject<Dinosaur>("lambeosaurus", new Dinosaur(2, 2, 2)),
                new FirebaseObject<Dinosaur>("stegosaurus", new Dinosaur(3, 3, 3))
            };

            entities.Should().BeEquivalentTo(expectation);
        }

        [TestMethod]
        public void NewTopLevelItemInsertedToNonEmptyCache()
        {
            // this should simulate when connection had already been established with some data populated when new top-level item arrived
            var dinosaurs = new List<FirebaseObject<Dinosaur>>(new[]
            {
                new FirebaseObject<Dinosaur>("lambeosaurus", new Dinosaur(2, 2, 2)),
                new FirebaseObject<Dinosaur>("stegosaurus", new Dinosaur(3, 3, 3))
            });

            var cache = new FirebaseCache<Dinosaur>(dinosaurs.ToDictionary(f => f.Key, f => f.Object));
            var trexData = @"{
    ""ds"": {
      ""height"" : 4,
      ""length"" : 4,
      ""weight"": 4
    }
}";

            var entities = cache.PushData("/trex", trexData).ToList();
            var trex = new FirebaseObject<Dinosaur>("trex", new Dinosaur(4, 4, 4));

            entities.Should().HaveCount(1);
            entities.First().Should().BeEquivalentTo(trex);
        }

        [TestMethod]
        public void SecondLevelItemWhichBelongsToExistingObjectChanged()
        {
            // this should simulate when some data of an existing object changed
            var dinosaurs = new List<FirebaseObject<Dinosaur>>(new[]
            {
                new FirebaseObject<Dinosaur>("lambeosaurus", new Dinosaur(2, 2, 2)),
                new FirebaseObject<Dinosaur>("stegosaurus", new Dinosaur(3, 3, 3))
            });

            var cache = new FirebaseCache<Dinosaur>(dinosaurs.ToDictionary(f => f.Key, f => f.Object));

            var stegosaurusds = @"
{
  ""height"" : 4,
  ""length"" : 4,
  ""weight"": 4
}";

            var entities = cache.PushData("/stegosaurus/ds", stegosaurusds).ToList();
            var stegosaurus = new FirebaseObject<Dinosaur>("stegosaurus", new Dinosaur(4, 4, 4));

            entities.Should().HaveCount(1);
            entities.First().Should().BeEquivalentTo(stegosaurus);
        }

        [TestMethod]
        public void PrimitiveItemWhichBelongsToExistingObjectChanged()
        {
            // this should simulate when some primitive data of an existing object changed
            var dinosaurs = new List<FirebaseObject<Dinosaur>>(new[]
            {
                new FirebaseObject<Dinosaur>("lambeosaurus", new Dinosaur(2, 2, 2)),
                new FirebaseObject<Dinosaur>("stegosaurus", new Dinosaur(3, 3, 3))
            });

            var cache = new FirebaseCache<Dinosaur>(dinosaurs.ToDictionary(f => f.Key, f => f.Object));

            var height = "4";

            var entities = cache.PushData("/stegosaurus/ds/height", height).ToList();
            var stegosaurus = new FirebaseObject<Dinosaur>("stegosaurus", new Dinosaur(4, 3, 3));

            entities.Should().HaveCount(1);
            entities.First().Should().BeEquivalentTo(stegosaurus);
        }

        [TestMethod]
        public void ObjectWithDictionaryPropertyInserted()
        {
            var jurassicPrague = new FirebaseObject<JurassicWorld>("jurassicPrague", new JurassicWorld());
            jurassicPrague.Object.Dinosaurs.Add("lambeosaurus", new Dinosaur(2, 2, 2));
            jurassicPrague.Object.Dinosaurs.Add("stegosaurus", new Dinosaur(3, 3, 3));

            var cache = new FirebaseCache<JurassicWorld>();
            var jurassicPragueJson = @"
{ 
  ""jurassicPrague"": {
      ""dinosaurs"" : {
          ""lambeosaurus"": {
            ""ds"": {
              ""height"" : 2,
              ""length"" : 2,
              ""weight"": 2
            }
          },
          ""stegosaurus"": {
            ""ds"": {
              ""height"" : 3,
              ""length"" : 3,
              ""weight"" : 3
            }
          }
        }
    }
}";
            var entities = cache.PushData("/", jurassicPragueJson).ToList();

            entities.Should().HaveCount(1);
            entities.First().Should().BeEquivalentTo(jurassicPrague);
        }

        [TestMethod]
        public void ObjectWithinDictionaryChanged()
        {
            var jurassicPrague = new FirebaseObject<JurassicWorld>("jurassicPrague", new JurassicWorld());
            jurassicPrague.Object.Dinosaurs.Add("lambeosaurus", new Dinosaur(2, 2, 2));
            jurassicPrague.Object.Dinosaurs.Add("stegosaurus", new Dinosaur(3, 3, 3));

            var cache = new FirebaseCache<JurassicWorld>(new Dictionary<string, JurassicWorld>()
                { { jurassicPrague.Key, jurassicPrague.Object } }
            );

            var stegosaurusds = @"
{ 
    ""height"" : 4,
    ""length"" : 4,
    ""weight"": 4
}";
            var entities = cache.PushData("/jurassicPrague/dinosaurs/stegosaurus/ds", stegosaurusds).ToList();

            entities.Should().HaveCount(1);
            entities.First().Should().BeEquivalentTo(jurassicPrague);
            jurassicPrague.Object.Dinosaurs["stegosaurus"].Dimensions.Should().BeEquivalentTo(new Dimensions { Height = 4, Length = 4, Weight = 4 });

            //var entities = cache.PushData("/jurassicPrague/dinosaurs/stegosaurus/Name", "").ToList();
        }

        [TestMethod]
        public void NestedDictionaryChanged()
        {
            var jurassicPrague = new FirebaseObject<JurassicWorld>("jurassicPrague", new JurassicWorld());
            jurassicPrague.Object.Dinosaurs.Add("lambeosaurus", new Dinosaur(2, 2, 2));
            jurassicPrague.Object.Dinosaurs.Add("stegosaurus", new Dinosaur(3, 3, 3));

            var cache = new FirebaseCache<JurassicWorld>(new Dictionary<string, JurassicWorld>()
                { { jurassicPrague.Key, jurassicPrague.Object } }
            );

            var trex = @"
{
    ""trex"": {
        ""ds"": {
            ""height"" : 5,
            ""length"" : 4,
            ""weight"": 4
        }
    }
}";
            var entities = cache.PushData("/jurassicPrague/dinosaurs/", trex).ToList();

            entities.Should().HaveCount(1);
            entities.First().Should().BeEquivalentTo(jurassicPrague);
            jurassicPrague.Object.Dinosaurs["trex"].Dimensions.Should().BeEquivalentTo(new Dimensions { Height = 5, Length = 4, Weight = 4 });
        }

        [TestMethod]
        public void ItemChangesInDictionaryOfPrimitiveBooleans()
        {
            var cache = new FirebaseCache<bool>();
            var boolDictionary = new [] { new FirebaseObject<bool>("a", true), new FirebaseObject<bool>("b", true), new FirebaseObject<bool>("c", false) };
            var bools = @"
{ 
    ""a"" : true,
    ""b"" : true,
    ""c"": false
}";

            var entities = cache.PushData("/", bools).ToList();
            entities.Should().BeEquivalentTo(boolDictionary);

            entities = cache.PushData("/d", "true").ToList();
            entities.First().Should().BeEquivalentTo(new FirebaseObject<bool>("d", true));

            entities = cache.PushData("/c", "true").ToList();
            entities.First().Should().BeEquivalentTo(new FirebaseObject<bool>("c", true));
        }

        [TestMethod]
        public void ItemChangesInDictionaryOfPrimitiveStrings()
        {
            var cache = new FirebaseCache<string>();
            var stringDictionary = new[] { new FirebaseObject<string>("a", "a"), new FirebaseObject<string>("b", "b"), new FirebaseObject<string>("c", "c") };
            var strings = @"
{ 
    ""a"" : ""a"",
    ""b"" : ""b"",
    ""c"": ""c""
}";

            var entities = cache.PushData("/", strings).ToList();
            entities.Should().BeEquivalentTo(stringDictionary);

            // firebase sends strings without double quotes
            entities = cache.PushData("/d", @"d").ToList();
            entities.First().Should().BeEquivalentTo(new FirebaseObject<string>("d", "d"));

            entities = cache.PushData("/c", @"cc").ToList();
            entities.First().Should().BeEquivalentTo(new FirebaseObject<string>("c", "cc"));
        }

        [TestMethod]
        public void ItemDeletedInDictionary()
        {
            var cache = new FirebaseCache<Dinosaur>();
            var dinosaurs = @"{
  ""lambeosaurus"": {
    ""ds"": {
      ""height"" : 2,
      ""length"" : 2,
      ""weight"": 2
    }
  },
  ""stegosaurus"": {
    ""ds"": {
      ""height"" : 3,
      ""length"" : 3,
      ""weight"" : 3
    }
  }
}";

            var entities = cache.PushData("/", dinosaurs).ToList();

            // delete top level item from dictionary
            entities = cache.PushData("/stegosaurus", " ").ToList();

            cache.Count().Should().Be(1);
            entities.Should().BeEquivalentTo(new[] { new FirebaseObject<Dinosaur>("stegosaurus", new Dinosaur(3, 3, 3)) });

            // delete a property - it should be set to null
            entities = cache.PushData("/lambeosaurus/ds", " ").ToList();

            cache.Count().Should().Be(1);
            entities.Should().BeEquivalentTo(new[] { new FirebaseObject<Dinosaur>("lambeosaurus", new Dinosaur { Dimensions = null }) });
        }

        [TestMethod]
        public void ItemPatchedInDictionary()
        {
            var cache = new FirebaseCache<Dinosaur>();
            var dinosaurs = @"{
  ""lambeosaurus"": {
    ""ds"": {
      ""height"" : 2,
      ""length"" : 2,
      ""weight"": 2
    }
  },
  ""stegosaurus"": {
    ""ds"": {
      ""height"" : 3,
      ""length"" : 3,
      ""weight"" : 3
    }
  }
}";

            var patch = @"
{
    ""height"" : 8,
}
";

            var entities = cache.PushData("/", dinosaurs).ToList();

            // delete top level item from dictionary
            entities = cache.PushData("/stegosaurus/ds", patch).ToList();

            entities.First().Should().BeEquivalentTo(new FirebaseObject<Dinosaur>("stegosaurus", new Dinosaur(8, 3, 3)));
        }

        [TestMethod]
        public void DictionaryCache()
        {
            var cache = new FirebaseCache<Dictionary<string, string>>();
            var data = @"
{
    ""a"": ""aa"",
    ""b"": ""bb""
}";

            var updateData = @"aaa";

            cache.PushData("root/", data).ToList();
            var entities = cache.PushData("root/a", updateData).ToList();

            entities.First().Should().BeEquivalentTo(new FirebaseObject<Dictionary<string, string>>("root", new Dictionary<string, string>()
            {
                ["a"] = "aaa",
                ["b"] = "bb",
            }));
        }



        [TestMethod]
        public void InitialPushOfEntireArrayToEmptyCache()
        {
            // Data that looks like an array will be returned as an array (without keys).
            // https://firebase.googleblog.com/2014/04/best-practices-arrays-in-firebase.html

            // this should simulate first time connection is made and all data is returned in a batch in a form of a dictionary
            var cache = new FirebaseCache<Dinosaur>();
            var dinosaurs = @"[
  {
    ""ds"": {
      ""height"" : 2,
      ""length"" : 2,
      ""weight"": 2
    }
  },
  {
    ""ds"": {
      ""height"" : 3,
      ""length"" : 3,
      ""weight"" : 3
    }
  }
]";

            var entities = cache.PushData("/", dinosaurs).ToList();
            var expectation = new[]
            {
                new FirebaseObject<Dinosaur>("0", new Dinosaur(2, 2, 2)),
                new FirebaseObject<Dinosaur>("1", new Dinosaur(3, 3, 3))
            };

            entities.Should().BeEquivalentTo(expectation);
        }

        [TestMethod]
        public void ExistingKeyShouldReplace()
        {
            var cache = new FirebaseCache<HNChanges>();
            var original = @"
                {
                    ""items"": [1, 2, 3],
                    ""profiles"": [""a"", ""b"", ""c""]
                }";
            var incoming = @"
                {
                    ""items"": [4, 5, 6],
                    ""profiles"": [""d"", ""e"", ""f""]
                }";
            var expectation = new []
            {
                new FirebaseObject<HNChanges>("updates", new HNChanges()
                    {
                        items = new List<long>() { 4, 5, 6 },
                        profiles = new List<string>() { "d", "e", "f" }
                    }
                )
            };

            cache.PushData("updates/", original).ToList();
            var entities = cache.PushData("updates/", incoming).ToList();

            entities.Should().BeEquivalentTo(expectation);
        }

        [TestMethod]
        public void AddedItemShouldBeAppendedToCollection()
        {
            var cache = new FirebaseCache<List<long>>();
            var original = @"[1, 2, 3]";
            var incoming = @"[1, 2, 3, 4]";
            var expectation = new []
            {
                new FirebaseObject<List<long>>("updates", new List<long>() { 1, 2, 3, 4 })
            };

            cache.PushData("updates/", original);
            var entities = cache.PushData("updates/", incoming);

            entities.Should().BeEquivalentTo(expectation);
        }

        [TestMethod]
        public void CanUpdateObjectInsideList()
        {
            var cache = new FirebaseCache<HandOfCards>();
            var original = @"
                {
                    ""Cards"": [
                        { ""Suit"": 0 },
                    ]
                }
            ";
            var newObject = @"
                {
                    ""Cards"": [
                        { ""Suit"": 2 },
                    ]
                }
            ";
            var expectation = new[] { new FirebaseObject<HandOfCards>("HandOfCards", JsonConvert.DeserializeObject<HandOfCards>(newObject)) };

            cache.PushData("HandOfCards/", original);
            var entities = cache.PushData("HandOfCards/Cards/0/Suit", "2");

            entities.Should().BeEquivalentTo(expectation);
        }

        [TestMethod]
        public void CanUpdateEnumInObject()
        {
            var cache = new FirebaseCache<Card>();
            var newObject = @"{ ""Suit"": 2 }";
            var expectation = new[] { new FirebaseObject<Card>("Card", JsonConvert.DeserializeObject<Card>(newObject)) };

            cache.PushData("Card/", @"{ ""Suit"": 0 }");
            var entities = cache.PushData("Card/Suit", "2");

            entities.Should().BeEquivalentTo(expectation);
        }
    }
}
