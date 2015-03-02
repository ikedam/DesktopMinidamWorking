using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopMinidamWorking
{
    /**
     *  アニメーション状態
     */
    public enum AnimationState
    {
        IdleDoNothing,  // ひま / ぼーっとしている
        IdleBallooning, // ひま / シャボン玉吹いている
        WorkingEasy,    // 働いている / ゆっくり働いている
        WorkingNormal,  // 働いている / 普通に働いている
        WorkingHard,    // 働いている / 激しく働いている
        WorkingResting, // 働いている / 休憩中
    }

    /**
     * アニメーションのキーフレームの遷移先定義
     */
    public class KeyFrameTransition
    {
        public int possibility; // 遷移確率 (100分率)
        public int frame;  // 遷移先のフレーム

        public KeyFrameTransition(int possibility, int frame)
        {
            this.possibility = possibility;
            this.frame = frame;
        }

        public KeyFrameTransition(int frame)
        {
            this.possibility = 100;
            this.frame = frame;
        }
    }

    /**
     * アニメーションのキーフレーム
     */
    public class KeyFrameInformation
    {
        public int frame;
        public string image;
        public KeyFrameTransition[] transitions;

        public KeyFrameInformation(int frame, string image)
        {
            this.frame = frame;
            this.image = image;
            this.transitions = new KeyFrameTransition[] { };
        }

        public KeyFrameInformation(int frame, KeyFrameTransition[] transitions)
        {
            this.frame = frame;
            this.image = null;
            this.transitions = transitions;
        }

        public KeyFrameInformation(int frame, int nextFrame)
        {
            this.frame = frame;
            this.image = null;
            this.transitions = new KeyFrameTransition[] { new KeyFrameTransition(nextFrame) };
        }
    }

    /**
     * アニメーションの遷移定義
     */
    public class AnimationTransition
    {
        public int possibility; // 遷移確率
        public AnimationState state;  // 遷移先の状態
        public AnimationTransition(int possibility, AnimationState state)
        {
            this.possibility = possibility;
            this.state = state;
        }
    }

    /**
     * アニメーション定義
     */
    public class AnimationDefinition
    {
        public AnimationState state;
        public int durationFramesMin;
        public int durationFramesMax;
        public string talkingImage;
        public AnimationTransition[] transitions;
        public KeyFrameInformation[] keyFrames;

        public AnimationDefinition(AnimationState state, int durationFramesMin, int durationFramesMax, string talkingImage, AnimationTransition[] transitions, KeyFrameInformation[] keyFrames)
        {
            this.state = state;
            this.durationFramesMin = durationFramesMin;
            this.durationFramesMax = durationFramesMax;
            this.talkingImage = talkingImage;
            this.transitions = transitions;
            this.keyFrames = keyFrames;
        }

        public KeyFrameInformation GetKeyFrame(int frame)
        {
            foreach (KeyFrameInformation keyInfo in keyFrames)
            {
                if (keyInfo.frame == frame)
                {
                    return keyInfo;
                }

                if (keyInfo.frame > frame)
                {
                    break;
                }
            }
            return null;
        }
    }

    class AnimationScenario
    {
        private static AnimationDefinition[] animations = {
            // 暇なとき
            new AnimationDefinition(
                AnimationState.IdleDoNothing,
                300, 600, "/images/minidam_talking03.png",
                new AnimationTransition[]{},
                new KeyFrameInformation[] {
                    new KeyFrameInformation(1, "/images/minidam_idle01.png"),
                    new KeyFrameInformation(31, "/images/minidam_idle02.png"),
                    new KeyFrameInformation(61, 1),
                }
            ),
            // 働いているとき
            new AnimationDefinition(
                AnimationState.WorkingNormal,
                300, 600, "/images/minidam_talking01.png",
                new AnimationTransition[]{},
                new KeyFrameInformation[] {
                    new KeyFrameInformation(1, "/images/minidam_work01.png"),
                    new KeyFrameInformation(16, "/images/minidam_work02.png"),
                    new KeyFrameInformation(31, 1),
                }
            ),
        };

        public AnimationDefinition GetAnimationDefinition(AnimationState state)
        {
            foreach (AnimationDefinition def in animations)
            {
                if (def.state == state)
                {
                    return def;
                }
            }
            return null;
        }
    }
}
