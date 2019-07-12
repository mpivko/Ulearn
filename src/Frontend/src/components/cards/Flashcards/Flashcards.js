import React, {Component} from 'react';
import PropTypes from "prop-types";
import styles from './flashcards.less';
import classNames from 'classnames';
import Button from "@skbkontur/react-ui/Button";
import Results from "./Results/Results";
import ProgressBar from "../ProgressBar/ProgressBar";
import api from "../../../api";
import {withRouter} from "react-router-dom";

const modalsStyles = {
	first: classNames(styles.modal),
	second: classNames(styles.modal, styles.secondModal),
	third: classNames(styles.modal, styles.thirdModal),
	fourth: classNames(styles.modal, styles.fourthModal)
};

const defaultStatistics = {
	notRated: 0,
	rate1: 0,
	rate2: 0,
	rate3: 0,
	rate4: 0,
	rate5: 0
};

class Flashcards extends Component {
	constructor(props) {
		super(props);
		const flashcards = this.props.flashcards;
		const statistics = Flashcards.countStatistics(flashcards);

		this.state = {
			showAnswer: false,
			currentIndex: 0,
			statistics: statistics,
			flashcards: flashcards
		}
	}

	componentDidMount() {
		document.getElementsByTagName('body')[0].classList.add(styles.overflow);
		document.addEventListener('keypress', this.handleKeyPress);
	}

	static countStatistics(flashcards) {
		const statistics = {...defaultStatistics};

		for (const card of flashcards) {
			statistics[card.rate]++;
		}

		return statistics;
	}

	componentWillUnmount() {
		document.removeEventListener('keypress', this.handleKeyPress);
		document.getElementsByTagName('body')[0].classList.remove(styles.overflow);
	}

	render() {
		const {flashcards, showAnswer, currentIndex, statistics} = this.state;
		const totalFlashcardsCount = flashcards.length;

		return (
			<div className={styles.overlay}>
				<div ref={(ref) => this.firstModal = ref} className={modalsStyles.first}>
					{flashcards && this.renderFlashcard(flashcards[currentIndex], showAnswer)}
				</div>
				<div ref={(ref) => this.secondModal = ref} className={modalsStyles.second}/>
				<div ref={(ref) => this.thirdModal = ref} className={modalsStyles.third}/>
				<div className={modalsStyles.fourth}/>
				{Flashcards.renderControlGuides()}
				<div className={styles.progressBarContainer}>
					<ProgressBar statistics={statistics} totalFlashcardsCount={totalFlashcardsCount}/>
				</div>
			</div>
		)
	}

	get courseId() {
		return this.props.match.params.courseId.toLowerCase();
	}

	handleKeyPress = (e) => {
		const code = e.key;
		const spaceChar = ' ';

		if (code === spaceChar) {
			this.showAnswer();
		} else if (code >= 1 && code <= 5 && this.state.showAnswer) {
			this.handleResultsClick(code);
		}
	};


	renderFlashcard({unitTitle = '', question = '', answer = ''}, showAnswer) {
		return (
			<div>
				<button tabIndex={1} className={styles.closeButton} onClick={this.props.onClose}>
					&times;
				</button>
				<h5 className={styles.unitTitle}>
					{unitTitle ? unitTitle : 'unknown'}
				</h5>
				{!showAnswer && this.renderFrontFlashcard(question)}
				{showAnswer && this.renderBackFlashcard(question, answer)}
			</div>
		);
	}

	renderFrontFlashcard(question) {
		return (
			<div className={styles.frontTextContainer}>
				<div className={styles.questionFront} dangerouslySetInnerHTML={{__html: question}}/>
				<div className={styles.showAnswerButtonContainer}>
					<Button size='large' use='primary' onClick={() => this.showAnswer()}>
						Показать ответ
					</Button>
				</div>
			</div>
		);
	}

	showAnswer() {
		this.resetAnimation();
		this.setState({
			showAnswer: true
		});
	}

	renderBackFlashcard(question, answer) {
		return (
			<div>
				<div className={styles.backTextContainer}>
					<div className={styles.questionBack} dangerouslySetInnerHTML={{__html: question}}/>
					<div dangerouslySetInnerHTML={{__html: answer}}/>
				</div>
				<Results handleClick={this.handleResultsClick}/>
			</div>
		);
	}

	static renderControlGuides() {
		return (
			<p className={styles.controlGuideContainer}>
				Используйте клавиатуру:
				<span>пробел</span> — показать ответ,
				<span>1</span>
				<span>2</span>
				<span>3</span>
				<span>4</span>
				<span>5</span> — поставить оценку
			</p>
		);
	}

	handleResultsClick = (rate) => {
		const {currentIndex, flashcards, statistics} = this.state;
		const currentCard = flashcards[currentIndex];
		const newRate = `rate${rate}`;

		api.cards.putFlashcardStatus(this.courseId, currentCard.id, newRate);

		statistics[currentCard.rate]--;
		statistics[newRate]++;
		currentCard.rate = newRate;

		this.setState({
			statistics: statistics
		});
		this.takeNextFlashcard();
	};

	takeNextFlashcard() {
		let {currentIndex, flashcards} = this.state;

		currentIndex++;

		if (currentIndex === flashcards.length) {
			currentIndex = 0;
		}

		this.setState({
			currentIndex: currentIndex,
			showAnswer: false
		});

		this.animateCards();
	}

	animateCards() {
		const {firstModal, secondModal, thirdModal} = this;
		const animationDuration = 700;

		firstModal.className = classNames(modalsStyles.second, styles.move);
		secondModal.className = classNames(modalsStyles.third, styles.move);
		thirdModal.className = classNames(modalsStyles.fourth, styles.move);

		setTimeout(() => {
			this.resetAnimation();
		}, animationDuration - animationDuration / 10);
	}

	resetAnimation() {
		const {firstModal, secondModal, thirdModal} = this;

		firstModal.className = classNames(modalsStyles.first);
		secondModal.className = classNames(modalsStyles.second);
		thirdModal.className = classNames(modalsStyles.third);
	}
}


Flashcards.propTypes = {
	flashcards: PropTypes.arrayOf(PropTypes.shape({
		id: PropTypes.string,
		question: PropTypes.string,
		answer: PropTypes.string,
		unitTitle: PropTypes.string,
		rate: PropTypes.string,
		unitId: PropTypes.string
	})).isRequired,
	match: PropTypes.object,
	onClose: PropTypes.func
};

export default withRouter(Flashcards);
