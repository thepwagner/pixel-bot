using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace crafty
{
    class Program
    {
        static void Main(string[] args)
        {
            const int pixelSize = 1;
            const int ClassMage = 8;

            Dictionary<string,VirtualKeyCode> keyBindings = new Dictionary<string,VirtualKeyCode>();
            keyBindings["Food"] = VirtualKeyCode.VK_3;
            keyBindings["Drink"] = VirtualKeyCode.VK_4;

            keyBindings["Frostbolt"] = VirtualKeyCode.VK_R;
            keyBindings["Mana Shield"] = VirtualKeyCode.VK_1;
            keyBindings["Arcane Intellect"] = VirtualKeyCode.VK_5;
            keyBindings["Frost Armor"] = VirtualKeyCode.VK_6;

            var rand = new Random();
            var sim = new InputSimulator();

            while (true) {
                Thread.Sleep(800 + rand.Next(200));

                // Capture screen:
                using var bitmap = new Bitmap(1920, 1080);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
                }

                var playerPixel = bitmap.GetPixel(0, 0);
                var playerHealth = (float)playerPixel.R / 255;
                var playerPower = (float)playerPixel.G / 255;
                var playerClass = (int)((float)playerPixel.B / 255 * 24);
                var playerCombat = playerPixel.B > 128;
                if (playerCombat) {
                    playerClass -= 12;
                }
                
                var targetPixel = bitmap.GetPixel(pixelSize, 0);
                var targetHostile = targetPixel.R > 128;
                var targetHealth = (float)targetPixel.G / 255;
                var targetMinDistance = (float)targetPixel.B / 255 * 45;

                List<string> buffNames = new List<string>();
                buffNames.Add("Food");
                buffNames.Add("Drinking");
                switch (playerClass)
                {
                    case ClassMage:
                        buffNames.Add("Arcane Intellect");
                        buffNames.Add("Frost Armor");
                        buffNames.Add("Mana Shield");
                        buffNames.Add("Ice Barrier");
                        break;
                }

                Dictionary<String,float> myBuffs = new Dictionary<string, float>();
                for (int buffPixelI=0; buffPixelI<Decimal.Ceiling((decimal)buffNames.Count/3); buffPixelI++) {
                    var buffPixel = bitmap.GetPixel(buffPixelI * pixelSize, pixelSize);

                    // rConsole.WriteLine(buffPixel.R + "," +  buffPixel.G + "," + buffPixel.B);
                    myBuffs[buffNames[buffPixelI*3]] = (float)buffPixel.R / 255 * 180;
                    if (buffNames.Count > buffPixelI*3+1) {
                        myBuffs[buffNames[buffPixelI*3+1]] = (float)buffPixel.G / 255 * 180;
                    }
                    if (buffNames.Count > buffPixelI*3+2) {
                        myBuffs[buffNames[buffPixelI*3+2]] = (float)buffPixel.B / 255 * 180;
                    }
                }
                
                // Dump state for debugging:
                // Console.WriteLine("-----------");
                // Console.WriteLine("Player: HP=" + playerHealth + ", MP=" + playerPower + ", Class=" + playerClass + ", Combat=" + playerCombat);
                // Console.WriteLine("Buffs: ");
                // foreach (string buff in buffNames)
                // {
                //     float buffDuration = 0;
                //     myBuffs.TryGetValue(buff, out buffDuration);
                //     Console.WriteLine(String.Format("  {0,3:0} {1}", buffDuration, buff));
                // }
                // Console.WriteLine("Target: Hostile=" + targetHostile + ", HP=" + targetHealth  + ", Distance=" + targetMinDistance);

                // If the target isn't hostile, do nothing
                if (!targetHostile) {
                    Console.WriteLine("  : no target");
                    goto nextTick;
                }
                if (targetHealth == 0) {
                    Console.WriteLine("  : target is dead");
                    goto nextTick;
                }

                Action<VirtualKeyCode,string> keyPress = (VirtualKeyCode key, string reason) => {
                    Console.WriteLine(String.Format("{0} : {1}", key, reason));
                    sim.Keyboard
                        .KeyDown(key)
                        .Sleep(40 + rand.Next(10))
                        .KeyUp(key)
                        .Sleep(400 + rand.Next(100));
                };

                float foodDuration = 0;
                myBuffs.TryGetValue("Food", out foodDuration);
                float drinkDuration = 0;
                myBuffs.TryGetValue("Drinking", out drinkDuration);

                // Are we in combat?
                if (!playerCombat) {
                    // No, check buffs:
                    switch (playerClass)
                    {
                        case ClassMage:
                            string[] longBuffs = {"Arcane Intellect", "Frost Armor"};
                            // TODO: ice barrier iff specced
                            foreach (string buff in longBuffs) {
                                float buffDuration = 0;
                                myBuffs.TryGetValue(buff, out buffDuration);
                                if (buffDuration < 120) {
                                    keyPress(keyBindings[buff], buff);
                                    goto nextTick;
                                }
                            }
                            break;
                    }

                    // Check HP/mana:
                    bool regen = false;
                    if (playerHealth < 0.75) {
                        if (foodDuration == 0) {
                            keyPress(keyBindings["Food"], "eat");
                            goto nextTick;
                        }
                        regen = true;
                    }
                    if (playerPower < 0.75 || (regen && playerPower < 0.85)) {
                        if (drinkDuration == 0) {
                            keyPress(keyBindings["Drink"], "drink");
                            goto nextTick;
                        }
                        regen = true;
                    }
                    if (regen || (foodDuration > 0 && playerHealth < 1) || (drinkDuration > 0 && playerPower < 0.9)) {
                        Console.WriteLine("  : waiting for regen...");
                        goto nextTick;
                    }
                }

                if (playerHealth == 1 && playerPower == 1 && (foodDuration > 0 || drinkDuration > 0)) {
                    keyPress(VirtualKeyCode.SPACE, "break regen");
                    goto nextTick;
                }

                // Check combat buffs
                switch (playerClass)
                {
                    case ClassMage:
                        // string[] shortBuffs = {"Mana Shield"};
                        // // TODO: ice barrier iff specced
                        // foreach (string buff in shortBuffs) {
                        //     float buffDuration = 0;
                        //     myBuffs.TryGetValue(buff, out buffDuration);
                        //     if (buffDuration == 0) {
                        //         keyPress(keyBindings[buff], buff);
                        //         goto nextTick;
                        //     }
                        // }
                        break;
                }

                // Begin DPS:
                switch (playerClass)
                {
                    case ClassMage:
                        if (targetMinDistance < 30) {
                            keyPress(keyBindings["Frostbolt"], "dps");
                        } else {
                            Console.WriteLine("move closer");
                        }
                        goto nextTick;
                }

                Console.WriteLine("  : TODO");

                // for(int i=0;i<5;i++){
                
                // }
                nextTick: {}
            }
        }
    }
}

