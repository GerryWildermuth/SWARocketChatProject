﻿using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SWARocketChat.Extensions;
using SWARocketChat.Models;
using SWARocketChat.Models.ManageViewModels;
using SWARocketChat.Services;

namespace SWARocketChat.Controllers
{
    [Authorize]
    [Route("Users")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        
        public UsersController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new IndexViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = StatusMessage,
                Id = user.Id,
                Userimage = user.UserImage,
                Password = user.PasswordHash
            };
            return View(model);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            if (model.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    throw new ApplicationException($"Unexpected error occurred setting email for user with ID '{user.Id}'.");
                }
            }
            if (model.Username != user.UserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Username);
                if (!setUserNameResult.Succeeded)
                {
                    throw new ApplicationException($"Unexpected error occurred setting UserName for user with ID '{user.Id}'.");
                }
            }

            if (model.Password != null && model.OldPassword != null)
            {
                var changePasswordResult =
                    await _userManager.ChangePasswordAsync(user, model.OldPassword, model.Password);
                if (!changePasswordResult.Succeeded)
                {
                    StatusMessage = "Error wrong Password ";
                    return RedirectToAction("Index", "Users");
                    //throw new ApplicationException(
                    //    $"Unexpected error occurred setting Password for user with ID '{user.Id}'.");
                }
            }

            if (model.Userimage != user.UserImage)
            {
                user.UserImage = Base64ImageConverter.ResizeBase64ImageString(model.Userimage, 150, 150);
                var userImage = await _userManager.UpdateAsync(user);
                if (!userImage.Succeeded)
                {
                    throw new ApplicationException(
                        $"Unexpected error occurred setting UserImage for user with ID '{user.Id}'.");
                }
            }

            StatusMessage = "Your profile has been updated";

            return RedirectToAction("Index","Users");
        }
        
        [HttpGet("Settings")]
        public IActionResult Settings()
        {
            return View();
        }
        [HttpGet("Security")]
        public IActionResult Security()
        {
            return View();
        }
        
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser user)
        {
            if (ModelState.IsValid)
            {
                if (user != null)
                {
                    user.UserImage= Base64ImageConverter.ResizeBase64ImageString(user.UserImage, 150, 150);
                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                        return RedirectToAction(nameof(Index));
                    AddErrors(result);
                }
            }
            return RedirectToAction("Index","Users",await _userManager.GetUserAsync(User));
        }

        // GET: Users/Delete/5
        [HttpGet("Delete")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(""+id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost("Delete"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _userManager.FindByIdAsync("" + id);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(Delete), id);
            }

            await _signInManager.SignOutAsync();
            await _userManager.DeleteAsync(user);
            return RedirectToAction("Login", "Account");
            //return RedirectToAction(nameof(Index));
        }
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
