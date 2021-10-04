mergeInto(LibraryManager.library, {
	OSMD_update: function (bpm, keys, lengths, key_count) {
		// create OSMD instance if first call
		if (document.osmd == null) {
			document.osmd = new opensheetmusicdisplay.OpenSheetMusicDisplay("osmd", { drawingParameters: "compacttight" });
		}

		// MusicXML header
		// TODO: use timewise rather than partwise?
		var xml_str = '<?xml version="1.0" encoding="UTF-8"?>\
			<!DOCTYPE score-partwise PUBLIC "-//Recordare//DTD MusicXML 2.0 Partwise//EN"\
			  "http://www.musicxml.org/dtds/partwise.dtd">\
			<score-partwise version="2.0">\
			  <defaults>\
				<scaling>\
				  <millimeters>2.1166666679999997</millimeters>\
				  <tenths>10</tenths>\
				</scaling>\
				<page-layout>\
				  <page-height>1320</page-height>\
				  <page-width>1020</page-width>\
				  <page-margins type="both">\
					<left-margin>59.583333333333336</left-margin>\
					<right-margin>56.66666666666667</right-margin>\
					<top-margin>96.66666666666667</top-margin>\
					<bottom-margin>80</bottom-margin>\
				  </page-margins>\
				</page-layout>\
				<system-layout>\
				  <system-margins>\
					<left-margin>59.583333333333336</left-margin>\
					<right-margin>26.666666666666668</right-margin>\
				  </system-margins>\
				  <system-distance>92.5</system-distance>\
				  <top-system-distance>145.83333333333334</top-system-distance>\
				</system-layout>\
				<staff-layout>\
				  <staff-distance>55</staff-distance>\
				</staff-layout>\
			  </defaults>\
			  <part-list>\
				<score-part id="P1">\
				  <part-name>Piano (right)</part-name>\
				  <part-abbreviation>Piano (right)</part-abbreviation>\
				  <score-instrument id="P1I1">\
					<instrument-name>Piano (right)</instrument-name>\
				  </score-instrument>\
				  <midi-instrument id="P1I1">\
					<midi-channel>1</midi-channel>\
					<midi-program>1</midi-program>\
				  </midi-instrument>\
				</score-part>\
			  </part-list>\
			  <part id="P1">\
				<measure>\
				  <attributes>\
					<divisions>64</divisions>\
					<key>\
					  <fifths>0</fifths>\
					  <mode>major</mode>\
					</key>\
					<time>\
					  <beats>4</beats>\
					  <beat-type>4</beat-type>\
					</time>\
					<clef number="1">\
					  <sign>G</sign>\
					  <line>2</line>\
					</clef>\
				  </attributes>\
				  <direction placement="above">\
					<direction-type>\
					  <metronome>\
						<beat-unit>quarter</beat-unit>\
						<per-minute>' + bpm + '</per-minute>\
					  </metronome>\
					</direction-type>\
					<sound tempo="' + bpm + '"/>\
				  </direction>';

		// convert key array into MusicXML
		const notes_per_measure = 4; // TODO: account for note types and time signatures
		for (var i = 0; i < key_count; ++i) {
			// measure bar if appropriate
			if (i > 0 && i % notes_per_measure == 0) {
				xml_str += '\
				  </measure>\
				  <measure>';
			}

			// note
			const semitones_from_c = [ 0, 2, 4, 5, 7, 9, 11 ];
			const semitones_per_octave = 12;
			const keys_per_octave = 7;
			console.assert(semitones_from_c.length == keys_per_octave);
			var key_val = HEAPU32[(keys >> 2) + i]; // see https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
			var length_val = HEAPU32[(lengths >> 2) + i];
			var note_semitones_from_c = key_val % semitones_per_octave;
			var note_val = semitones_from_c.indexOf(note_semitones_from_c);
			var semitone_offset = 0;
			if (note_val == -1) {
				semitone_offset = 1; // TODO: pick between sharp/flat based on major/minor key
				note_val = semitones_from_c.indexOf(note_semitones_from_c - semitone_offset);
			}
			var note_letter = String.fromCharCode(((note_val + 2) % keys_per_octave) + 'A'.charCodeAt(0)); // see https://stackoverflow.com/questions/36129721/convert-number-to-alphabet-letter
			var note_octave = key_val / semitones_per_octave - 1; // NOTE offset: middle-C (MIDI key 60) in MusicXML is the start of octave 4 rather than 5
			xml_str += '\
				  <note>\
					<pitch>\
					  <step>' + note_letter + '</step>\
					  <alter>' + semitone_offset + '</alter>\
					  <octave>' + note_octave + '</octave>\
					</pitch>\
					<duration>' + length_val + '</duration>\
					<voice>1</voice>\
					<type>' + (length_val == 1 ? '64th' : length_val == 2 ? '32nd' : length_val == 4 ? '16th' : length_val == 8 ? 'eighth' : length_val == 16 ? 'quarter' : length_val == 32 ? 'half' : length_val == 64 ? 'whole' : '') + '</type>\
					<accidental>' + (semitone_offset > 0 ? 'sharp' : semitone_offset < 0 ? 'flat' : '')/*TODO: account for key signature*/ + '</accidental>\
				  </note>';
			// TODO: <beam>/<chord/>/<dot/>/<{p/mp/mf/f}/>
		}

		// MusicXML footer
		xml_str += '\
				  <barline location="right">\
					<bar-style>light-heavy</bar-style>\
				  </barline>\
				</measure>\
			  </part>\
			</score-partwise>';

		// load and render
		document.osmd.load(xml_str).then(function() {
			document.osmd.render();
		});
	},
});
