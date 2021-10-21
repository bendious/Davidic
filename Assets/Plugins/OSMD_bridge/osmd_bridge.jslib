mergeInto(LibraryManager.library, {
	UpdateInternal: function (element_id, title, instrument_names, instrument_count, key_fifths, key_mode, note_count, times, keys, lengths, note_channels, bpm) {
		const inputArrayUint = function (array, index) {
			return HEAPU32[(array >> 2) + index]; // see https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
		};

		// create OSMD instance if necessary
		var id_str = Pointer_stringify(element_id);
		var is_untimed = (bpm == 0);
		if (document.osmds == null) {
			document.osmds = {};
		}
		if (!(id_str in document.osmds)) {
			document.osmds[id_str] = new opensheetmusicdisplay.OpenSheetMusicDisplay(id_str, { drawingParameters: is_untimed ? "compacttight" : "compact" });
		}

		// constants
		/*const*/var sixtyfourths_per_quarter = 16;
		/*const*/var sixtyfourths_beam_max = 8;
		/*const*/var semitones_from_c = [ 0, 2, 4, 5, 7, 9, 11 ];
		/*const*/var semitones_per_octave = 12;
		/*const*/var keys_per_octave = 7;
		console.assert(semitones_from_c.length == keys_per_octave);
		/*const*/var types_by_length = {
			1: ['64th', ''],
			2: ['32nd', ''],
			3: ['32nd', '<dot/>'],
			4: ['16th', ''],
			6: ['16th', '<dot/>'],
			8: ['eighth', ''],
			12: ['eighth', '<dot/>'],
			16: ['quarter', ''],
			24: ['quarter', '<dot/>'],
			32: ['half', ''],
			48: ['half', '<dot/>'],
			64: ['whole', ''],
		};

		// gather input into object array
		var note_objects = [[]];
		for (var note_idx = 0; note_idx < note_count; ++note_idx) {
			var time_cur = inputArrayUint(times, note_idx);
			var key_cur = inputArrayUint(keys, note_idx);
			var length_cur = inputArrayUint(lengths, note_idx);
			var channel_cur = inputArrayUint(note_channels, note_idx);

			// expand channel array if necessary, get previous note
			while (note_objects.length <= channel_cur) {
				note_objects.push([]);
			}
			var note_list = note_objects[channel_cur];
			var note_prev = (note_list.length > 0) ? note_list[note_list.length - 1] : undefined;

			// update/add note
			if (note_prev != undefined && time_cur == note_prev.time && length_cur == note_prev.length) {
				note_prev.keys.add(key_cur);
			} else {
				var note_new = {
					time: time_cur,
					keys: new Set([ key_cur ]), // TODO: ensure duplicate keys aren't added code-side?
					length: length_cur,
					ties: [],
				};
				note_list.push(note_new);
			}
		}
		console.assert(note_objects.length == instrument_count);
		console.log("Original notes: " + JSON.stringify(note_objects)); // NOTE the strinification to preserve the original state

		// split notes crossing a measure boundary or of uneven length
		var length_per_measure = (is_untimed ? Number.POSITIVE_INFINITY : 64); // TODO: account for different time signatures
		for (const note_list of note_objects) {
			for (var list_idx = 0; list_idx < note_list.length; ++list_idx) {
				var note_obj = note_list[list_idx];
				var time_val_next = note_obj.time + note_obj.length;
				var length_post = parseInt(time_val_next) % length_per_measure;

				if ((length_post <= 0 || length_post >= note_obj.length) && (note_obj.length <= 0 || note_obj.length in types_by_length)) {
					// don't need to split this note
					continue;
				}

				if (length_post == note_obj.length) {
					// get longest standard/dotted length less than the current length
					var length_itr;
					for (length_itr = note_obj.length; length_itr > 0 && !(length_itr in types_by_length); --length_itr); // TODO: efficiency?
					length_post = length_itr;
				}

				// shorten and tie existing note
				note_obj.length -= length_post;
				time_val_next -= length_post;
				note_obj.ties.push('start');

				// add new tied note (after any harmony notes that come before it)
				var idx_new;
				for (idx_new = list_idx + 1; idx_new < note_list.length && note_list[idx_new].time < time_val_next; ++idx_new);
				var note_new = {
					time: time_val_next,
					keys: note_obj.keys,
					length: length_post,
					ties: [ 'stop' ],
				};
				note_list.splice(idx_new, 0, note_new);

				// re-process the shortened note next iteration in case it is now an odd length
				--list_idx;
			}
		}
		console.log(note_objects);

		// MusicXML header
		// TODO: use timewise rather than partwise?
		var xml_str = '<?xml version="1.0" encoding="UTF-8"?>\n\
			<!DOCTYPE score-partwise PUBLIC "-//Recordare//DTD MusicXML 2.0 Partwise//EN"\n\
				"http://www.musicxml.org/dtds/partwise.dtd">\n\
			<score-partwise version="2.0">\n\
				<part-list>';
		for (var channel_idx = 0; channel_idx < instrument_count; ++channel_idx) {
			var instrument_name_str = Pointer_stringify(inputArrayUint(instrument_names, channel_idx)); // TODO: don't assume string pointers are uints?
			xml_str += '\n\
					<score-part id="' + instrument_name_str + '">\n\
						<part-name>' + instrument_name_str + '</part-name>\n\
						<midi-instrument id="' + instrument_name_str + 'I1">\n\
							<midi-channel>' + channel_idx + '</midi-channel>\n\
							<midi-program>0</midi-program>\n\
						</midi-instrument>\n\
					</score-part>';
		}
		xml_str += '\n\
			</part-list>';

		var per_note_str = (is_untimed ? '\t\t\t\t\t\t\t<notehead>x</notehead>\n' : '');

		for (var channel_idx = 0; channel_idx < instrument_count; ++channel_idx) {
			var note_list = note_objects[channel_idx];

			var instrument_name_str = Pointer_stringify(inputArrayUint(instrument_names, channel_idx)); // TODO: don't assume string pointers are uints?
			xml_str += '\n\
				<part id="' + instrument_name_str + '">' + (is_untimed ? '\n\
				<measure>\n\
					<attributes>\n\
						<key print-object="no"></key>\n\
						<time print-object="no"></time>\n\
						<clef>\n\
							<sign>percussion</sign>\n\
							<staff-lines>5</staff-lines>\n\
						</clef>\n\
					</attributes>\n\
					<direction placement="above">\n\
						<direction-type>\n\
							<words>' + Pointer_stringify(title) + '</words>\n\
						</direction-type>\n\
					</direction>' : '\n\
				<measure>\n\
					<attributes>\n\
						<key>\n\
							<fifths>' + key_fifths + '</fifths>\n\
							<mode>' + Pointer_stringify(key_mode) + '</mode>\n\
						</key>\n\
						<time>\n\
							<beats>4</beats>\n\
							<beat-type>4</beat-type>\n\
						</time>\n\
						<clef number="1">\n\
							<sign>G</sign>\n\
							<line>2</line>\n\
						</clef>\n\
						<divisions>16</divisions>/*TODO: base on time signature?*/\n\
					</attributes>\n\
					<direction placement="above">\n\
						<direction-type>\n\
							<metronome>\n\
								<beat-unit>quarter</beat-unit>\n\
								<per-minute>' + bpm + '</per-minute>\n\
							</metronome>\n\
						</direction-type>\n\
						<sound tempo="' + bpm + '"/>\n\
					</direction>');

			// accumulators / inter-note memory
			var time_val_prev = -1;
			var length_val_prev = -1;
			var overlap_amount = 0; // TODO: support more than two overlapping voices?

			// per-note
			for (var note_idx = 0; note_idx < note_list.length; ++note_idx) {
				var note_obj = note_list[note_idx];
				var time_val = note_obj.time;
				var length_val = note_obj.length;

				// overlap w/ previous note(s) if necessary
				var note_prev_end = time_val_prev + length_val_prev;
				if (time_val < note_prev_end) {
					overlap_amount = note_prev_end - time_val;
					xml_str += '\n\
						<backup>\n\
							<duration>' + overlap_amount + '</duration>\n\
						</backup>';
				}

				// add barline if appropriate
				var new_measure = (overlap_amount <= 0 && time_val > 0 && time_val % length_per_measure == 0);
				if (new_measure) {
					xml_str += '\n\
						</measure>\n\
						<measure>';
				}

				var key_idx = 0;
				for (const key_val of note_obj.keys.values()) {
					// per-note tags
					var pitch_tag = (is_untimed ? 'unpitched' : 'pitch');
					var pitch_prefix = (is_untimed ? 'display-' : '');
					var note_semitones_from_c = key_val % semitones_per_octave;
					var note_val = semitones_from_c.indexOf(note_semitones_from_c);
					var semitone_offset = 0;
					if (note_val == -1) {
						semitone_offset = (key_fifths < 0) ? -1 : 1;
						note_val = semitones_from_c.indexOf(note_semitones_from_c - semitone_offset);
					}
					var note_letter = String.fromCharCode(((note_val + 2) % keys_per_octave) + 'A'.charCodeAt(0)); // see https://stackoverflow.com/questions/36129721/convert-number-to-alphabet-letter
					var note_octave = Math.floor(key_val / semitones_per_octave) - 1; // NOTE offset: middle-C (MIDI key 60) in MusicXML is the start of octave 4 rather than 5
					var type_and_dot_str = (length_val in types_by_length) ? types_by_length[length_val] : [ 'ERROR', '' ];
					var type_str = type_and_dot_str[0];
					var dot_str = type_and_dot_str[1];
					var beam_before = !new_measure && (note_idx > 0 && length_val <= sixtyfourths_beam_max && length_val_prev <= sixtyfourths_beam_max);
					var beam_after = (note_idx + 1 < note_list.length && length_val <= sixtyfourths_beam_max && note_list[note_idx + 1].length <= sixtyfourths_beam_max); // TODO: detect measure end?
					var beam_str = (beam_before && beam_after) ? 'continue' : (beam_before ? 'end' : (beam_after ? 'begin' : ''));
					var accidental_str = (semitone_offset > 0 ? 'sharp' : semitone_offset < 0 ? 'flat' : '');

					// add to XML
					xml_str += '\n\
						<note>\n\
							' + (key_idx > 0 ? '<chord/>' : '') +
							'<' + pitch_tag + '>\n\
								<' + pitch_prefix + 'step>' + note_letter + '</' + pitch_prefix + 'step>\n\
								<alter>' + semitone_offset + '</alter>\n\
								<' + pitch_prefix + 'octave>' + note_octave + '</' + pitch_prefix + 'octave>\n\
							</' + pitch_tag + '>\n\
							<duration>' + length_val + '</duration>\n\
							<voice>' + (overlap_amount > 0 ? 2 : 1) + '</voice>\n\
							<type>' + type_str + '</type>' + dot_str + '\n'
							+ (beam_str == '' ? '' : '\t\t\t\t\t\t\t<beam number="1">' + beam_str + '</beam>\n')
							+ (accidental_str == '' ? '' : ('\t\t\t\t\t\t\t<accidental>' + accidental_str + '</accidental>\n'))
							+ per_note_str;
					if (note_obj.ties.length > 0) {
						xml_str += '\t\t\t\t\t\t\t<notations>\n';
						for (const tie_type_str of note_obj.ties) {
							xml_str += '\t\t\t\t\t\t\t\t<tied type="' + tie_type_str + '"/>\n';
						}
						xml_str += '\t\t\t\t\t\t\t</notations>\n';
					}
					xml_str += '\t\t\t\t\t\t</note>';
					// TODO: <{p/mp/mf/f}/>?

					++key_idx;
				}

				time_val_prev = time_val;
				length_val_prev = length_val;
				overlap_amount -= length_val;
			}

			if (!is_untimed) {
				xml_str += '\n\
					<barline location="right">\n\
						<bar-style>light-heavy</bar-style>\n\
					</barline>';
			}
			xml_str += '\n\
					</measure>\n\
				</part>';
		}

		// footer
		xml_str += '\n\
			</score-partwise>';

		// output
		console.log(xml_str); // TEMP?
		document.osmds[id_str].load(xml_str).then(function() {
			document.osmds[id_str].render();
		});
	},
});
