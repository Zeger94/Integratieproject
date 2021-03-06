﻿using Domain.Post;
using Domain.TextGain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL
{
    public interface IPostManager
    {
        Task SyncDataAsync();
        void AddPost(Post post);
        List<Post> getAllPosts();
    }
}
