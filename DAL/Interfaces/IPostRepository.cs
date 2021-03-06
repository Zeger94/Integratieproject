﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Post;

namespace DAL
{
    public interface IPostRepository
    {
        void AddPosts(List<Post> posts);
        void AddPost(Post post);
        List<Post> getAllPosts();
    }
}
