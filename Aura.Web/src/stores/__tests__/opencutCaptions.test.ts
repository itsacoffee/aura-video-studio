/**
 * OpenCut Captions Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutCaptionsStore, DEFAULT_CAPTION_STYLE } from '../opencutCaptions';

describe('OpenCutCaptionsStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useOpenCutCaptionsStore.setState({
      tracks: [],
      selectedTrackId: null,
      selectedCaptionId: null,
      editingTrackId: null,
      editingCaptionId: null,
    });
  });

  describe('Track Management', () => {
    it('should add a new track', () => {
      const { addTrack } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('English Subtitles', 'en-US');

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks.length).toBe(1);
      expect(state.tracks[0].id).toBe(trackId);
      expect(state.tracks[0].name).toBe('English Subtitles');
      expect(state.tracks[0].language).toBe('en-US');
      expect(state.selectedTrackId).toBe(trackId);
    });

    it('should create track with default style', () => {
      const { addTrack, getTrackById } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.defaultStyle).toEqual(DEFAULT_CAPTION_STYLE);
    });

    it('should remove a track', () => {
      const { addTrack, removeTrack } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      useOpenCutCaptionsStore.getState().removeTrack(trackId);

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks.length).toBe(0);
      expect(state.selectedTrackId).toBeNull();
    });

    it('should update track properties', () => {
      const { addTrack, updateTrack, getTrackById } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      useOpenCutCaptionsStore.getState().updateTrack(trackId, {
        name: 'Updated Track',
        language: 'fr-FR',
        position: 'top',
      });

      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.name).toBe('Updated Track');
      expect(track?.language).toBe('fr-FR');
      expect(track?.position).toBe('top');
    });

    it('should duplicate a track', () => {
      const { addTrack, addCaption, duplicateTrack } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Original Track');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Test caption');

      const newTrackId = useOpenCutCaptionsStore.getState().duplicateTrack(trackId);

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks.length).toBe(2);
      expect(newTrackId).not.toBeNull();

      const newTrack = state.getTrackById(newTrackId!);
      expect(newTrack?.name).toBe('Original Track (Copy)');
      expect(newTrack?.captions.length).toBe(1);
      expect(newTrack?.captions[0].text).toBe('Test caption');
    });

    it('should toggle track visibility', () => {
      const { addTrack, toggleTrackVisibility, getTrackById } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      expect(useOpenCutCaptionsStore.getState().getTrackById(trackId)?.visible).toBe(true);

      useOpenCutCaptionsStore.getState().toggleTrackVisibility(trackId);
      expect(useOpenCutCaptionsStore.getState().getTrackById(trackId)?.visible).toBe(false);

      useOpenCutCaptionsStore.getState().toggleTrackVisibility(trackId);
      expect(useOpenCutCaptionsStore.getState().getTrackById(trackId)?.visible).toBe(true);
    });

    it('should toggle track lock', () => {
      const { addTrack, toggleTrackLock, getTrackById } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      expect(useOpenCutCaptionsStore.getState().getTrackById(trackId)?.locked).toBe(false);

      useOpenCutCaptionsStore.getState().toggleTrackLock(trackId);
      expect(useOpenCutCaptionsStore.getState().getTrackById(trackId)?.locked).toBe(true);
    });
  });

  describe('Caption Management', () => {
    it('should add a caption to a track', () => {
      const { addTrack, addCaption, getTrackById } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello world');

      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.captions.length).toBe(1);
      expect(track?.captions[0].id).toBe(captionId);
      expect(track?.captions[0].text).toBe('Hello world');
      expect(track?.captions[0].startTime).toBe(0);
      expect(track?.captions[0].endTime).toBe(2);
    });

    it('should not add caption to locked track', () => {
      const { addTrack, toggleTrackLock, addCaption, getTrackById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      useOpenCutCaptionsStore.getState().toggleTrackLock(trackId);

      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Test');

      expect(captionId).toBeNull();
      expect(useOpenCutCaptionsStore.getState().getTrackById(trackId)?.captions.length).toBe(0);
    });

    it('should sort captions by start time', () => {
      const { addTrack, addCaption, getTrackById } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      useOpenCutCaptionsStore.getState().addCaption(trackId, 4, 6, 'Third');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'First');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 2, 4, 'Second');

      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.captions[0].text).toBe('First');
      expect(track?.captions[1].text).toBe('Second');
      expect(track?.captions[2].text).toBe('Third');
    });

    it('should remove a caption', () => {
      const { addTrack, addCaption, removeCaption, getTrackById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Test');

      useOpenCutCaptionsStore.getState().removeCaption(trackId, captionId!);

      expect(useOpenCutCaptionsStore.getState().getTrackById(trackId)?.captions.length).toBe(0);
    });

    it('should update a caption', () => {
      const { addTrack, addCaption, updateCaption, getCaptionById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Original');

      useOpenCutCaptionsStore.getState().updateCaption(trackId, captionId!, {
        text: 'Updated text',
        speaker: 'John',
      });

      const caption = useOpenCutCaptionsStore.getState().getCaptionById(trackId, captionId!);
      expect(caption?.text).toBe('Updated text');
      expect(caption?.speaker).toBe('John');
    });

    it('should move a caption', () => {
      const { addTrack, addCaption, moveCaption, getCaptionById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Test');

      useOpenCutCaptionsStore.getState().moveCaption(trackId, captionId!, 5);

      const caption = useOpenCutCaptionsStore.getState().getCaptionById(trackId, captionId!);
      expect(caption?.startTime).toBe(5);
      expect(caption?.endTime).toBe(7); // Duration preserved
    });

    it('should resize a caption', () => {
      const { addTrack, addCaption, resizeCaption, getCaptionById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Test');

      useOpenCutCaptionsStore.getState().resizeCaption(trackId, captionId!, 1, 5);

      const caption = useOpenCutCaptionsStore.getState().getCaptionById(trackId, captionId!);
      expect(caption?.startTime).toBe(1);
      expect(caption?.endTime).toBe(5);
    });

    it('should not resize if start >= end', () => {
      const { addTrack, addCaption, resizeCaption, getCaptionById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Test');

      useOpenCutCaptionsStore.getState().resizeCaption(trackId, captionId!, 5, 3);

      const caption = useOpenCutCaptionsStore.getState().getCaptionById(trackId, captionId!);
      expect(caption?.startTime).toBe(0);
      expect(caption?.endTime).toBe(2);
    });

    it('should split a caption', () => {
      const { addTrack, addCaption, splitCaption, getTrackById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 4, 'Full text');

      const newId = useOpenCutCaptionsStore.getState().splitCaption(trackId, captionId!, 2);

      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.captions.length).toBe(2);
      expect(track?.captions[0].endTime).toBe(2);
      expect(track?.captions[1].startTime).toBe(2);
      expect(newId).not.toBeNull();
    });

    it('should merge two captions', () => {
      const { addTrack, addCaption, mergeCaptions, getTrackById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      const caption1Id = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'First');
      const caption2Id = useOpenCutCaptionsStore.getState().addCaption(trackId, 2, 4, 'Second');

      useOpenCutCaptionsStore.getState().mergeCaptions(trackId, caption1Id!, caption2Id!);

      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.captions.length).toBe(1);
      expect(track?.captions[0].text).toBe('First Second');
      expect(track?.captions[0].startTime).toBe(0);
      expect(track?.captions[0].endTime).toBe(4);
    });
  });

  describe('Caption Styling', () => {
    it('should update track default style', () => {
      const { addTrack, updateTrackStyle, getTrackById } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      useOpenCutCaptionsStore.getState().updateTrackStyle(trackId, {
        fontSize: 32,
        color: '#FF0000',
      });

      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.defaultStyle.fontSize).toBe(32);
      expect(track?.defaultStyle.color).toBe('#FF0000');
    });

    it('should update caption style override', () => {
      const { addTrack, addCaption, updateCaptionStyle, getCaptionById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Test');

      useOpenCutCaptionsStore.getState().updateCaptionStyle(trackId, captionId!, {
        fontSize: 48,
        fontWeight: 700,
      });

      const caption = useOpenCutCaptionsStore.getState().getCaptionById(trackId, captionId!);
      expect(caption?.style?.fontSize).toBe(48);
      expect(caption?.style?.fontWeight).toBe(700);
    });

    it('should reset caption style', () => {
      const { addTrack, addCaption, updateCaptionStyle, resetCaptionStyle, getCaptionById } =
        useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Test');

      useOpenCutCaptionsStore.getState().updateCaptionStyle(trackId, captionId!, {
        fontSize: 48,
      });
      useOpenCutCaptionsStore.getState().resetCaptionStyle(trackId, captionId!);

      const caption = useOpenCutCaptionsStore.getState().getCaptionById(trackId, captionId!);
      expect(caption?.style).toBeUndefined();
    });
  });

  describe('Import/Export', () => {
    it('should import SRT content', () => {
      const { addTrack, importSRT, getTrackById } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      const srtContent = `1
00:00:00,000 --> 00:00:02,000
Hello world

2
00:00:02,000 --> 00:00:04,000
How are you`;

      const imported = useOpenCutCaptionsStore.getState().importSRT(trackId, srtContent);

      expect(imported).toBe(2);
      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.captions.length).toBe(2);
      expect(track?.captions[0].text).toBe('Hello world');
      expect(track?.captions[1].text).toBe('How are you');
    });

    it('should import VTT content', () => {
      const { addTrack, importVTT, getTrackById } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');

      const vttContent = `WEBVTT

1
00:00:00.000 --> 00:00:02.000
Hello world

2
00:00:02.000 --> 00:00:04.000
How are you`;

      const imported = useOpenCutCaptionsStore.getState().importVTT(trackId, vttContent);

      expect(imported).toBe(2);
      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.captions.length).toBe(2);
    });

    it('should export to SRT format', () => {
      const { addTrack, addCaption, exportToSRT } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello world');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 2, 4, 'How are you');

      const srt = useOpenCutCaptionsStore.getState().exportToSRT(trackId);

      expect(srt).toContain('1\n00:00:00,000 --> 00:00:02,000\nHello world');
      expect(srt).toContain('2\n00:00:02,000 --> 00:00:04,000\nHow are you');
    });

    it('should export to VTT format', () => {
      const { addTrack, addCaption, exportToVTT } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello world');

      const vtt = useOpenCutCaptionsStore.getState().exportToVTT(trackId);

      expect(vtt).toContain('WEBVTT');
      expect(vtt).toContain('00:00:00.000 --> 00:00:02.000');
      expect(vtt).toContain('Hello world');
    });
  });

  describe('Query Operations', () => {
    it('should get captions in range', () => {
      const { addTrack, addCaption, getCaptionsInRange } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'First');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 2, 4, 'Second');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 5, 7, 'Third');

      const captions = useOpenCutCaptionsStore.getState().getCaptionsInRange(trackId, 1, 3);

      expect(captions.length).toBe(2);
      expect(captions[0].text).toBe('First');
      expect(captions[1].text).toBe('Second');
    });

    it('should get visible tracks', () => {
      const { addTrack, toggleTrackVisibility, getVisibleTracks } =
        useOpenCutCaptionsStore.getState();
      addTrack('Track 1');
      const track2Id = useOpenCutCaptionsStore.getState().addTrack('Track 2');
      addTrack('Track 3');

      useOpenCutCaptionsStore.getState().toggleTrackVisibility(track2Id);

      const visibleTracks = useOpenCutCaptionsStore.getState().getVisibleTracks();
      expect(visibleTracks.length).toBe(2);
    });

    it('should get active caption at time', () => {
      const { addTrack, addCaption, getActiveCaption } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'First');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 2, 4, 'Second');

      const active1 = useOpenCutCaptionsStore.getState().getActiveCaption(1);
      const active2 = useOpenCutCaptionsStore.getState().getActiveCaption(3);
      const active3 = useOpenCutCaptionsStore.getState().getActiveCaption(5);

      expect(active1?.caption.text).toBe('First');
      expect(active2?.caption.text).toBe('Second');
      expect(active3).toBeNull();
    });
  });

  describe('Navigation', () => {
    it('should go to next caption', () => {
      const { addTrack, addCaption, goToNextCaption } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'First');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 3, 5, 'Second');

      const next = useOpenCutCaptionsStore.getState().goToNextCaption(1);

      expect(next?.text).toBe('Second');
      expect(useOpenCutCaptionsStore.getState().selectedCaptionId).toBe(next?.id);
    });

    it('should go to previous caption', () => {
      const { addTrack, addCaption, goToPreviousCaption } = useOpenCutCaptionsStore.getState();
      const trackId = addTrack('Test Track');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'First');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 3, 5, 'Second');

      // At time 6, the previous caption (starting before 6) with the highest start time is 'Second' at 3
      const prev = useOpenCutCaptionsStore.getState().goToPreviousCaption(6);

      expect(prev?.text).toBe('Second');

      // From time 3, the previous caption is 'First' at 0
      const prev2 = useOpenCutCaptionsStore.getState().goToPreviousCaption(2.5);
      expect(prev2?.text).toBe('First');
    });
  });
});
