﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Raven.Database.Extensions;
using Xunit;
using Raven.Client.Document;
using Raven.Database.Server;
using System.Threading;
using System.IO;

namespace Raven.Client.Tests.Querying
{
    public class UsingDynamicQueryWithRemoteServer : RemoteClientTest, IDisposable
    {   
		private readonly string path;
        private readonly int port;

		public UsingDynamicQueryWithRemoteServer()
		{
            port = 8080;
            path = GetPath("TestDb");
			NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(8080);
		}

		#region IDisposable Members

		public void Dispose()
		{
            IOExtensions.DeleteDirectory(path);
		}

		#endregion

        [Fact]
        public void CanPerformDynamicQueryUsingClientLinqQuery()
        {
            var blogOne = new Blog
            {
                Title = "one",
                Category = "Ravens"
            };
            var blogTwo = new Blog
            {
                Title = "two",
                Category = "Rhinos"
            };
            var blogThree = new Blog
            {
                Title = "three",
                Category = "Rhinos"
            };

            using (var server = GetNewServer(port, path))
            {
                var store = new DocumentStore { Url = "http://localhost:" + port };
                store.Initialize();

                using (var s = store.OpenSession())
                {
                    s.Store(blogOne);
                    s.Store(blogTwo);
                    s.Store(blogThree);
                    s.SaveChanges();
                }

                using (var s = store.OpenSession())
                {
                    var results = s.Query<Blog>()
                        .Customize(x => x.WaitForNonStaleResultsAsOfNow())
                        .Where(x => x.Category == "Rhinos" && x.Title.Length == 3)
                        .ToArray();

                    var blogs = s.Advanced.DynamicLuceneQuery<Blog>()
                        .Where("Category:Rhinos AND Title.Length:3")
                        .ToArray();

                    Assert.Equal(1, results.Length);
                    Assert.Equal("two", results[0].Title);
                    Assert.Equal("Rhinos", results[0].Category);
                }
            }
        }

        [Fact]
        public void CanPerformDynamicQueryUsingClientLuceneQuery()
        {
            var blogOne = new Blog
            {
                Title = "one",
                Category = "Ravens"
            };
            var blogTwo = new Blog
            {
                Title = "two",
                Category = "Rhinos"
            };
            var blogThree = new Blog
            {
                Title = "three",
                Category = "Rhinos"
            };

            using (var server = GetNewServer(port, path))
            {
                var store = new DocumentStore { Url = "http://localhost:" + port };
                store.Initialize();

                using (var s = store.OpenSession())
                {
                    s.Store(blogOne);
                    s.Store(blogTwo);
                    s.Store(blogThree);
                    s.SaveChanges();
                }

                using (var s = store.OpenSession())
                {
                    var results = s.Advanced.DynamicLuceneQuery<Blog>()
                        .Where("Title.Length:3 AND Category:Rhinos")
                        .WaitForNonStaleResultsAsOfNow().ToArray();

                    Assert.Equal(1, results.Length);
                    Assert.Equal("two", results[0].Title);
                    Assert.Equal("Rhinos", results[0].Category);
                }
            }
        }

        [Fact]
        public void CanPerformProjectionUsingClientLinqQuery()
        {
            using (var server = GetNewServer(port, path))
            {
                var store = new DocumentStore { Url = "http://localhost:" + port };
                store.Initialize();

                var blogOne = new Blog
                {
                    Title = "one",
                    Category = "Ravens",
                    Tags = new Tag[] { 
                         new Tag() { Name = "tagOne"},
                         new Tag() { Name = "tagTwo"}
                    }
                };

                using (var s = store.OpenSession())
                {
                    s.Store(blogOne);
                    s.SaveChanges();
                }

                using (var s = store.OpenSession())
                {
                    var results = s.Query<Blog>()
                        .Where(x => x.Title == "one" && x.Tags.Any(y => y.Name == "tagTwo"))
                        .Select(x => new
                        {
                            x.Category,
                            x.Title
                        })
                        .Single();

                    Assert.Equal("one", results.Title);
                    Assert.Equal("Ravens", results.Category);
                }
            }
        }

        public class Blog
        {
            public User User
            {
                get;
                set;
            }

            public string Title
            {
                get;
                set;
            }

            public Tag[] Tags
            {
                get;
                set;
            }

            public string Category
            {
                get;
                set;
            }
        }

        public class Tag
        {
            public string Name
            {
                get;
                set;
            }
        }

        public class User
        {
            public string Name
            {
                get;
                set;
            }
        }
    }
}
